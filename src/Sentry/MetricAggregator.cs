using System;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Protocol.Metrics;

namespace Sentry;

internal class MetricAggregator : IMetricAggregator
{
    private readonly SentryOptions _options;
    private readonly Action<IEnumerable<Metric>> _captureMetrics;
    private readonly Action<CodeLocations> _captureCodeLocations;
    private readonly TimeSpan _flushInterval;

    private readonly SemaphoreSlim _codeLocationLock = new(1,1);

    private readonly CancellationTokenSource _shutdownSource;
    private volatile bool _disposed;

    // The key for this dictionary is the Timestamp for the bucket, rounded down to the nearest RollupInSeconds... so it
    // aggregates all of the metrics data for a particular time period. The Value is a dictionary for the metrics,
    // each of which has a key that uniquely identifies it within the time period
    internal ConcurrentDictionary<long, ConcurrentDictionary<string, Metric>> Buckets => _buckets.Value;

    private readonly Lazy<ConcurrentDictionary<long, ConcurrentDictionary<string, Metric>>> _buckets
        = new(() => new ConcurrentDictionary<long, ConcurrentDictionary<string, Metric>>());

    private long _lastClearedStaleLocations = DateTimeOffset.UtcNow.GetDayBucketKey();
    private readonly ConcurrentDictionary<long, HashSet<MetricResourceIdentifier>> _seenLocations = new();
    private Dictionary<long, Dictionary<MetricResourceIdentifier, SentryStackFrame>> _pendingLocations = new();

    private Task LoopTask { get; }

    /// <summary>
    /// MetricAggregator constructor.
    /// </summary>
    /// <param name="options">The <see cref="SentryOptions"/></param>
    /// <param name="captureMetrics">The callback to be called to transmit aggregated metrics</param>
    /// <param name="captureCodeLocations">The callback to be called to transmit new code locations</param>
    /// <param name="shutdownSource">A <see cref="CancellationTokenSource"/></param>
    /// <param name="disableLoopTask">
    /// A boolean value indicating whether the Loop to flush metrics should run, for testing only.
    /// </param>
    /// <param name="flushInterval">An optional flushInterval, for testing only</param>
    public MetricAggregator(SentryOptions options, Action<IEnumerable<Metric>> captureMetrics,
        Action<CodeLocations> captureCodeLocations, CancellationTokenSource? shutdownSource = null,
        bool disableLoopTask = false, TimeSpan? flushInterval = null)
    {
        _options = options;
        _captureMetrics = captureMetrics;
        _captureCodeLocations = captureCodeLocations;
        _shutdownSource = shutdownSource ?? new CancellationTokenSource();
        _flushInterval = flushInterval ?? TimeSpan.FromSeconds(5);

        if (disableLoopTask)
        {
            // We can stop the loop from running during testing
            _options.LogDebug("LoopTask disabled.");
            LoopTask = Task.CompletedTask;
        }
        else
        {
            options.LogDebug("Starting MetricsAggregator.");
            LoopTask = Task.Run(RunLoopAsync);
        }
    }

    internal static string GetMetricBucketKey(MetricType type, string metricKey, MeasurementUnit unit,
        IDictionary<string, string>? tags)
    {
        var typePrefix = type.ToStatsdType();
        var serializedTags = tags?.ToUtf8Json() ?? string.Empty;

        return $"{typePrefix}_{metricKey}_{unit}_{serializedTags}";
    }

    /// <inheritdoc cref="IMetricAggregator.Increment"/>
    public void Increment(string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null,
        int stackLevel = 1) => Emit(MetricType.Counter, key, value, unit, tags, timestamp, stackLevel + 1);

    /// <inheritdoc cref="IMetricAggregator.Gauge"/>
    public void Gauge(string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null,
        int stackLevel = 1) => Emit(MetricType.Gauge, key, value, unit, tags, timestamp, stackLevel + 1);

    /// <inheritdoc cref="IMetricAggregator.Distribution"/>
    public void Distribution(string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null,
        int stackLevel = 1) => Emit(MetricType.Distribution, key, value, unit, tags, timestamp, stackLevel + 1);

    /// <inheritdoc cref="IMetricAggregator.Set"/>
    public void Set(string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null,
        int stackLevel = 1) => Emit(MetricType.Set, key, value, unit, tags, timestamp, stackLevel + 1);

    /// <inheritdoc cref="IMetricAggregator.Timing"/>
    public void Timing(string key,
        double value,
        MeasurementUnit.Duration unit = MeasurementUnit.Duration.Second,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null,
        int stackLevel = 1) => Emit(MetricType.Distribution, key, value, unit, tags, timestamp, stackLevel + 1);

    private readonly object _emitLock = new object();

    private void Emit(
        MetricType type,
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null,
        int stackLevel = 1
    )
    {
        timestamp ??= DateTimeOffset.UtcNow;
        unit ??= MeasurementUnit.None;

        Func<string, Metric> addValuesFactory = type switch
        {
            MetricType.Counter => _ => new CounterMetric(key, value, unit.Value, tags, timestamp),
            MetricType.Gauge => _ => new GaugeMetric(key, value, unit.Value, tags, timestamp),
            MetricType.Distribution => _ => new DistributionMetric(key, value, unit.Value, tags, timestamp),
            MetricType.Set => _ => new SetMetric(key, (int)value, unit.Value, tags, timestamp),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown MetricType")
        };

        var timeBucket = Buckets.GetOrAdd(
            timestamp.Value.GetTimeBucketKey(),
            _ => new ConcurrentDictionary<string, Metric>()
        );

        timeBucket.AddOrUpdate(
            GetMetricBucketKey(type, key, unit.Value, tags),
            addValuesFactory,
            (_, metric) =>
            {
                // This prevents multiple threads from trying to mutate the metric at the same time. The only other
                // operations performed against metrics are adding one to the bucket (guaranteed to be atomic due to
                // the use of a ConcurrentDictionary for the timeBucket) and removing buckets entirely.
                //
                // With a very small flushShift (e.g. 0.0) it might be possible for a metric to be emitted to a bucket
                // that was removed after a flush, in which case that metric.Add(value) would never make it to Sentry.
                // We've never seen this happen in unit testing (where we always set the flushShift to 0.0) so this
                // remains only a theoretical possibility of data loss (not confirmed). If this becomes a real problem
                // and we need to guarantee delivery of every metric.Add, we'll need to build a more complex mechanism
                // to coordinate flushing with emission.
                lock(metric)
                {
                    metric.Add(value);
                }
                return metric;
            });

        if (_options.ExperimentalMetrics is { EnableCodeLocations: true })
        {
            RecordCodeLocation(type, key, unit.Value, stackLevel + 1, timestamp.Value);
        }
    }

    internal void RecordCodeLocation(
        MetricType type,
        string key,
        MeasurementUnit unit,
        int stackLevel,
        DateTimeOffset timestamp
    )
    {
        var startOfDay = timestamp.GetDayBucketKey();
        var metaKey = new MetricResourceIdentifier(type, key, unit);
        var seenToday = _seenLocations.GetOrAdd(startOfDay,_ => []);

        _codeLocationLock.Wait();
        try
        {
            // Group metadata by day to make flushing more efficient.
            if (!seenToday.Add(metaKey))
            {
                // If we've seen the location, we don't want to create a stack trace etc. again. It could be a different
                // location with the same metaKey but the alternative would be to generate the stack trace every time a
                // metric is recorded, which would impact performance too much.
                return;
            }

            if (GetCodeLocation(stackLevel + 1) is not { } location)
            {
                return;
            }

            if (!_pendingLocations.TryGetValue(startOfDay, out var todaysLocations))
            {
                todaysLocations = new Dictionary<MetricResourceIdentifier, SentryStackFrame>();
                _pendingLocations[startOfDay] = todaysLocations;
            }

            todaysLocations[metaKey] = location;
        }
        finally
        {
            _codeLocationLock.Release();
        }
    }

    internal SentryStackFrame? GetCodeLocation(int stackLevel)
    {
        var stackTrace = new StackTrace(true);
        var frames = DebugStackTrace.Create(_options, stackTrace, false).Frames;
         return (frames.Count >= stackLevel)
            ? frames[^(stackLevel + 1)]
            : null;
    }

    private async Task RunLoopAsync()
    {
        _options.LogDebug("MetricsAggregator Started.");

        using var shutdownTimeout = new CancellationTokenSource();
        var shutdownRequested = false;

        try
        {
            while (!shutdownTimeout.IsCancellationRequested)
            {
                // If the cancellation was signaled, run until the end of the queue or shutdownTimeout
                try
                {
                    await Task.Delay(_flushInterval, _shutdownSource.Token).ConfigureAwait(false);
                }
                // Cancellation requested and no timeout allowed, so exit even if there are more items
                catch (OperationCanceledException) when (_options.ShutdownTimeout == TimeSpan.Zero)
                {
                    _options.LogDebug("Exiting immediately due to 0 shutdown timeout.");

                    await shutdownTimeout.CancelAsync().ConfigureAwait(false);

                    return;
                }
                // Cancellation requested, scheduled shutdown
                catch (OperationCanceledException)
                {
                    _options.LogDebug(
                        "Shutdown scheduled. Stopping by: {0}.",
                        _options.ShutdownTimeout);

                    shutdownTimeout.CancelAfterSafe(_options.ShutdownTimeout);

                    shutdownRequested = true;
                }

                await FlushAsync(shutdownRequested, shutdownTimeout.Token).ConfigureAwait(false);

                if (shutdownRequested)
                {
                    return;
                }
            }
        }
        catch (Exception e)
        {
            _options.LogFatal(e, "Exception in the Metric Aggregator.");
            throw;
        }
    }

    private readonly SemaphoreSlim _flushLock = new(1, 1);

    /// <inheritdoc cref="IMetricAggregator.FlushAsync"/>
    public async Task FlushAsync(bool force = true, CancellationToken cancellationToken = default)
    {
        try
        {
            await _flushLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            foreach (var key in GetFlushableBuckets(force))
            {
                cancellationToken.ThrowIfCancellationRequested();

                _options.LogDebug("Flushing metrics for bucket {0}", key);
                if (!Buckets.TryRemove(key, out var bucket))
                {
                    continue;
                }

                _captureMetrics(bucket.Values);
                _options.LogDebug("Metric flushed for bucket {0}", key);
            }

            foreach (var (timestamp, locations) in FlushableLocations())
            {
                cancellationToken.ThrowIfCancellationRequested();

                _options.LogDebug("Flushing code locations: ", timestamp);
                var codeLocations = new CodeLocations(timestamp, locations);
                _captureCodeLocations(codeLocations);
                _options.LogDebug("Code locations flushed: ", timestamp);
            }

            ClearStaleLocations();
        }
        catch (OperationCanceledException)
        {
            _options.LogInfo("Shutdown token triggered. Exiting metric aggregator.");
        }
        catch (Exception exception)
        {
            _options.LogError(exception, "Error processing metrics.");
        }
        finally
        {
            _flushLock.Release();
        }
    }

    /// <summary>
    /// Returns the keys for any buckets that are ready to be flushed (i.e. are for periods before the cutoff)
    /// </summary>
    /// <param name="force">Forces all buckets to be flushed, ignoring the cutoff</param>
    /// <returns>
    /// An enumerable containing the keys for any buckets that are ready to be flushed
    /// </returns>
    internal IEnumerable<long> GetFlushableBuckets(bool force = false)
    {
        if (!_buckets.IsValueCreated)
        {
            yield break;
        }

        if (force)
        {
            // Return all the buckets in this case
            foreach (var key in Buckets.Keys)
            {
                yield return key;
            }
        }
        else
        {
            var cutoff = MetricHelper.GetCutoff();
            foreach (var key in Buckets.Keys)
            {
                var bucketTime = DateTimeOffset.FromUnixTimeSeconds(key);
                if (bucketTime < cutoff)
                {
                    yield return key;
                }
            }
        }
    }

    private Dictionary<long, Dictionary<MetricResourceIdentifier, SentryStackFrame>> FlushableLocations()
    {
        _codeLocationLock.Wait();
        try
        {
            var result = _pendingLocations;
            _pendingLocations = new Dictionary<long, Dictionary<MetricResourceIdentifier, SentryStackFrame>>();
            return result;
        }
        finally
        {
            _codeLocationLock.Release();
        }
    }

    /// <summary>
    /// Clear out stale seen locations once a day
    /// </summary>
    private void ClearStaleLocations()
    {
        var now = DateTimeOffset.UtcNow;
        var today = now.GetDayBucketKey();
        if (_lastClearedStaleLocations == today)
        {
            return;
        }
        // Allow 60 seconds for all code locations to be sent at the transition from one day to the next
        const int staleGraceInMinutes = 1;
        if (now.Minute < staleGraceInMinutes)
        {
            return;
        }

        foreach (var dailyValues in _seenLocations.Keys.ToArray())
        {
            if (dailyValues < today)
            {
                _seenLocations.TryRemove(dailyValues, out _);
            }
        }
        _lastClearedStaleLocations = today;
    }

    /// <inheritdoc cref="IAsyncDisposable.DisposeAsync"/>
    public async ValueTask DisposeAsync()
    {
        _options.LogDebug("Disposing MetricAggregator.");

        if (_disposed)
        {
            _options.LogDebug("Already disposed MetricAggregator.");
            return;
        }

        _disposed = true;

        try
        {
            // Request the LoopTask stop.
            await _shutdownSource.CancelAsync().ConfigureAwait(false);

            // Now wait for the Loop to stop.
            // NOTE: While non-intuitive, do not pass a timeout or cancellation token here.  We are waiting for
            // the _continuation_ of the method, not its _execution_.  If we stop waiting prematurely, this may cause
            // unexpected behavior in client applications.
            await LoopTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _options.LogDebug("Stopping the Metric Aggregator due to a cancellation.");
        }
        catch (Exception exception)
        {
            _options.LogError(exception, "Async Disposing the Metric Aggregator threw an exception.");
        }
        finally
        {
            _flushLock.Dispose();
            _shutdownSource.Dispose();
            LoopTask.Dispose();
        }
    }

    public void Dispose()
    {
        try
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception exception)
        {
            _options.LogError(exception, "Disposing the Metric Aggregator threw an exception.");
        }
    }
}

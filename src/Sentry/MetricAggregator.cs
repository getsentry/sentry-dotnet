using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Protocol.Metrics;

namespace Sentry;

internal class MetricAggregator : IMetricAggregator, IDisposable
{
    private readonly SentryOptions _options;
    private readonly Action<IEnumerable<Metric>> _captureMetrics;
    private readonly Action<CodeLocations> _captureCodeLocations;
    private readonly TimeSpan _flushInterval;

    private readonly CancellationTokenSource _shutdownSource;
    private volatile bool _disposed;

    // The key for this dictionary is the Timestamp for the bucket, rounded down to the nearest RollupInSeconds... so it
    // aggregates all of the metrics data for a particular time period. The Value is a dictionary for the metrics,
    // each of which has a key that uniquely identifies it within the time period
    internal ConcurrentDictionary<long, ConcurrentDictionary<string, Metric>> Buckets => _buckets.Value;

    private readonly Lazy<ConcurrentDictionary<long, ConcurrentDictionary<string, Metric>>> _buckets
        = new(() => new ConcurrentDictionary<long, ConcurrentDictionary<string, Metric>>());

    private readonly HashSet<(long, MetricResourceIdentifier)> _seenLocations = new();
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
        var typePrefix = type switch
        {
            MetricType.Counter => "c",
            MetricType.Gauge => "g",
            MetricType.Distribution => "d",
            MetricType.Set => "s",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        var serializedTags = tags?.ToUtf8Json() ?? string.Empty;

        return $"{typePrefix}_{metricKey}_{unit}_{serializedTags}";
    }

    /// <inheritdoc cref="IMetricAggregator.Increment"/>
    public void Increment(
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTime? timestamp = null,
        int stackLevel = 0
    ) => Emit(MetricType.Counter, key, value, unit, tags, timestamp, stackLevel + 1);

    /// <inheritdoc cref="IMetricAggregator.Gauge"/>
    public void Gauge(
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTime? timestamp = null,
        int stackLevel = 0
    ) => Emit(MetricType.Gauge, key, value, unit, tags, timestamp, stackLevel + 1);

    /// <inheritdoc cref="IMetricAggregator.Distribution"/>
    public void Distribution(
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTime? timestamp = null,
        int stackLevel = 0
    ) => Emit(MetricType.Distribution, key, value, unit, tags, timestamp, stackLevel + 1);

    /// <inheritdoc cref="IMetricAggregator.Set"/>
    public void Set(
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTime? timestamp = null,
        int stackLevel = 0
    ) => Emit(MetricType.Set, key, value, unit, tags, timestamp, stackLevel + 1);

    /// <inheritdoc cref="IMetricAggregator.Timing"/>
    public void Timing(
        string key,
        double value,
        MeasurementUnit.Duration unit = MeasurementUnit.Duration.Second,
        IDictionary<string, string>? tags = null,
        DateTime? timestamp = null,
        int stackLevel = 0
    ) => Emit(MetricType.Distribution, key, value, unit, tags, timestamp, stackLevel + 1);

    private readonly object _emitLock = new object();

    private void Emit(
        MetricType type,
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTime? timestamp = null,
        int stackLevel = 0
    )
    {
        timestamp ??= DateTime.UtcNow;
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

        lock (_emitLock)
        {
            timeBucket.AddOrUpdate(
                GetMetricBucketKey(type, key, unit.Value, tags),
                addValuesFactory,
                (_, metric) =>
                {
                    metric.Add(value);
                    return metric;
                });
        }

        if (_options.ExperimentalMetrics is { EnableCodeLocations: true })
        {
            RecordCodeLocation(type, key, unit.Value, stackLevel + 1, timestamp.Value);
        }
    }

    private readonly ReaderWriterLockSlim _codeLocationLock = new();

    private void RecordCodeLocation(
        MetricType type,
        string key,
        MeasurementUnit unit,
        int stackLevel,
        DateTime timestamp
    )
    {
        var startOfDay = timestamp.GetDayBucketKey();
        var metaKey = new MetricResourceIdentifier(type, key, unit);

        _codeLocationLock.EnterUpgradeableReadLock();
        try
        {
            if (_seenLocations.Contains((startOfDay, metaKey)))
            {
                return;
            }
            _codeLocationLock.EnterWriteLock();
            try
            {
                // Group metadata by day to make flushing more efficient.
                _seenLocations.Add((startOfDay, metaKey));
                if (GetCodeLocation(stackLevel + 1) is not { } location)
                {
                    return;
                }

                if (!_pendingLocations.ContainsKey(startOfDay))
                {
                    _pendingLocations[startOfDay] = new Dictionary<MetricResourceIdentifier, SentryStackFrame>();
                }
                _pendingLocations[startOfDay][metaKey] = location;
            }
            finally
            {
                _codeLocationLock.ExitWriteLock();
            }
        }
        finally
        {
            _codeLocationLock.ExitUpgradeableReadLock();
        }
    }

    internal SentryStackFrame? GetCodeLocation(int stackLevel)
    {
        var stackTrace = new StackTrace(false);
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
                if (!shutdownRequested)
                {
                    try
                    {
                        await Task.Delay(_flushInterval, shutdownTimeout.Token).ConfigureAwait(false);
                    }
                    // Cancellation requested and no timeout allowed, so exit even if there are more items
                    catch (OperationCanceledException) when (_options.ShutdownTimeout == TimeSpan.Zero)
                    {
                        _options.LogDebug("Exiting immediately due to 0 shutdown timeout.");

                        shutdownTimeout.Cancel();

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
                }

                if (shutdownRequested || !Flush(false))
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

    private readonly object _flushLock = new();

    /// <summary>
    /// Flushes any flushable metrics and/or code locations.
    /// If <paramref name="force"/> is true then the cutoff is ignored and all metrics are flushed.
    /// </summary>
    /// <param name="force">Forces all buckets to be flushed, ignoring the cutoff</param>
    /// <returns>False if a shutdown is requested during flush, true otherwise</returns>
    internal bool Flush(bool force = true)
    {
        try
        {
            // We don't want multiple flushes happening concurrently... which might be possible if the regular flush loop
            // triggered a flush at the same time ForceFlush is called
            lock (_flushLock)
            {
                foreach (var key in GetFlushableBuckets(force))
                {
                    _options.LogDebug("Flushing metrics for bucket {0}", key);
                    if (Buckets.TryRemove(key, out var bucket))
                    {
                        _captureMetrics(bucket.Values);
                        _options.LogDebug("Metric flushed for bucket {0}", key);
                    }
                }

                foreach (var (timestamp, locations) in FlushableLocations())
                {
                    // There needs to be one envelope item per timestamp.
                    var codeLocations = new CodeLocations(timestamp, locations);
                    _captureCodeLocations(codeLocations);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _options.LogInfo("Shutdown token triggered. Time to exit.");
            return false;
        }
        catch (Exception exception)
        {
            _options.LogError(exception, "Error while processing metric aggregates.");
        }

        return true;
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

    Dictionary<long, Dictionary<MetricResourceIdentifier, SentryStackFrame>> FlushableLocations()
    {
        _codeLocationLock.EnterWriteLock();
        try
        {
            var result = _pendingLocations;
            _pendingLocations = new Dictionary<long, Dictionary<MetricResourceIdentifier, SentryStackFrame>>();
            return result;
        }
        finally
        {
            _codeLocationLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Stops the background worker and waits for it to empty the queue until 'shutdownTimeout' is reached
    /// </summary>
    /// <inheritdoc />
    public void Dispose()
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
            _shutdownSource.Cancel();

            // Now wait for the Loop to stop.
            // NOTE: While non-intuitive, do not pass a timeout or cancellation token here.  We are waiting for
            // the _continuation_ of the method, not its _execution_.  If we stop waiting prematurely, this may cause
            // unexpected behavior in client applications.
            LoopTask.Wait();
        }
        catch (OperationCanceledException)
        {
            _options.LogDebug("Stopping the Metric Aggregator due to a cancellation.");
        }
        catch (Exception exception)
        {
            _options.LogError(exception, "Stopping the Metric Aggregator threw an exception.");
        }
        finally
        {
            _shutdownSource.Dispose();
            LoopTask.Dispose();
        }
    }
}

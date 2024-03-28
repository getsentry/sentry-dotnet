using Sentry.Extensibility;
using Sentry.Force.Crc32;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Protocol.Metrics;

namespace Sentry;

internal class MetricAggregator : IMetricAggregator
{
    internal const string DisposingMessage = "Disposing MetricAggregator.";
    internal const string AlreadyDisposedMessage = "Already disposed MetricAggregator.";
    internal const string CancelledMessage = "Stopping the Metric Aggregator due to a cancellation.";
    internal const string ShutdownScheduledMessage = "Shutdown scheduled. Stopping by: {0}.";
    internal const string ShutdownImmediatelyMessage = "Exiting immediately due to 0 shutdown timeout.";
    internal const string FlushShutdownMessage = "Shutdown token triggered. Exiting metric aggregator.";

    private readonly SentryOptions _options;
    private readonly IMetricHub _metricHub;

    private readonly SemaphoreSlim _codeLocationLock = new(1, 1);
    private readonly ReaderWriterLockSlim _bucketsLock = new ReaderWriterLockSlim();

    private readonly CancellationTokenSource _shutdownSource;
    private volatile bool _disposed;

    // The key for this dictionary is the Timestamp for the bucket, rounded down to the nearest RollupInSeconds... so it
    // aggregates all of the metrics data for a particular time period. The Value is a dictionary for the metrics,
    // each of which has a key that uniquely identifies it within the time period
    internal Dictionary<long, ConcurrentDictionary<string, Metric>> Buckets => _buckets.Value;

    private readonly Lazy<Dictionary<long, ConcurrentDictionary<string, Metric>>> _buckets
        = new(() => new Dictionary<long, ConcurrentDictionary<string, Metric>>());

    internal long _lastClearedStaleLocations = DateTimeOffset.UtcNow.GetDayBucketKey();
    internal readonly ConcurrentDictionary<long, HashSet<MetricResourceIdentifier>> _seenLocations = new();
    internal Dictionary<long, Dictionary<MetricResourceIdentifier, SentryStackFrame>> _pendingLocations = new();

    internal readonly Task _loopTask;

    internal MetricAggregator(SentryOptions options, IMetricHub metricHub,
        CancellationTokenSource? shutdownSource = null, bool disableLoopTask = false)
    {
        _options = options;
        _metricHub = metricHub;
        _shutdownSource = shutdownSource ?? new CancellationTokenSource();

        if (disableLoopTask)
        {
            // We can stop the loop from running during testing
            _options.LogDebug("LoopTask disabled.");
            _loopTask = Task.CompletedTask;
        }
        else
        {
            options.LogDebug("Starting MetricsAggregator.");
            _loopTask = Task.Run(RunLoopAsync);
        }
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

    /// <inheritdoc cref="IMetricAggregator.Set(string,int,MeasurementUnit?,System.Collections.Generic.IDictionary{string,string},DateTimeOffset?,int)"/>
    public void Set(string key,
        int value,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null,
        int stackLevel = 1) => Emit(MetricType.Set, key, value, unit, tags, timestamp, stackLevel + 1);

    /// <inheritdoc cref="IMetricAggregator.Set(string,string,MeasurementUnit?,System.Collections.Generic.IDictionary{string,string},DateTimeOffset?,int)"/>
    public void Set(string key,
        string value,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null,
        int stackLevel = 1)
    {
        // Compute the CRC32 hash of the value as byte array and cast it to a 32-bit signed integer
        // Mask the lower 32 bits to ensure the result fits within the 32-bit integer range
        var hash = (int)(Crc32Algorithm.Compute(Encoding.UTF8.GetBytes(value)) & 0xFFFFFFFF);

        Emit(MetricType.Set, key, hash, unit, tags, timestamp, stackLevel + 1);
    }

    /// <inheritdoc cref="IMetricAggregator.Timing"/>
    public virtual void Timing(string key,
        double value,
        MeasurementUnit.Duration unit = MeasurementUnit.Duration.Second,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null,
        int stackLevel = 1) => Emit(MetricType.Distribution, key, value, unit, tags, timestamp, stackLevel + 1);

    /// <inheritdoc cref="IMetricAggregator.StartTimer"/>
    public IDisposable StartTimer(string key, MeasurementUnit.Duration unit = MeasurementUnit.Duration.Second,
        IDictionary<string, string>? tags = null, int stackLevel = 1)
        => new Timing(this, _metricHub, _options, key, unit, tags, stackLevel + 1);

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

        var updatedTags = tags != null ? new Dictionary<string, string>(tags) : new Dictionary<string, string>();
        updatedTags.AddIfNotNullOrEmpty("release", _options.Release);
        updatedTags.AddIfNotNullOrEmpty("environment", _options.Environment);
        var span = _metricHub.GetSpan();
        if (span?.GetTransaction() is { } transaction)
        {
            updatedTags.AddIfNotNullOrEmpty("transaction", transaction.TransactionName);
        }

        Func<string, Metric> addValuesFactory = type switch
        {
            MetricType.Counter => _ => new CounterMetric(key, value, unit.Value, updatedTags, timestamp),
            MetricType.Gauge => _ => new GaugeMetric(key, value, unit.Value, updatedTags, timestamp),
            MetricType.Distribution => _ => new DistributionMetric(key, value, unit.Value, updatedTags, timestamp),
            MetricType.Set => _ => new SetMetric(key, (int)value, unit.Value, updatedTags, timestamp),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown MetricType")
        };

        var timeBucket = GetOrAddTimeBucket(timestamp.Value.GetTimeBucketKey());

        timeBucket.AddOrUpdate(
            MetricHelper.GetMetricBucketKey(type, key, unit.Value, updatedTags),
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
                lock (metric)
                {
                    metric.Add(value);
                }
                return metric;
            });

        if (_options.Metrics is { EnableCodeLocations: true })
        {
            RecordCodeLocation(type, key, unit.Value, stackLevel + 1, timestamp.Value);
        }

        switch (span)
        {
            case TransactionTracer transactionTracer:
                transactionTracer.MetricsSummary.Add(type, key, value, unit, tags);
                break;
            case SpanTracer spanTracer:
                spanTracer.MetricsSummary.Add(type, key, value, unit, tags);
                break;
        }
    }

    private ConcurrentDictionary<string, Metric> GetOrAddTimeBucket(long bucketKey)
    {
        _bucketsLock.EnterUpgradeableReadLock();
        try
        {
            if (Buckets.TryGetValue(bucketKey, out var existingBucket))
            {
                return existingBucket;
            }

            _bucketsLock.EnterWriteLock();
            try
            {
                // Check again in case another thread added the bucket while we were waiting for the write lock
                if (Buckets.TryGetValue(bucketKey, out existingBucket))
                {
                    return existingBucket;
                }

                var timeBucket = new ConcurrentDictionary<string, Metric>();
                Buckets[bucketKey] = timeBucket;
                return timeBucket;
            }
            finally
            {
                _bucketsLock.ExitWriteLock();
            }
        }
        finally
        {
            _bucketsLock.ExitUpgradeableReadLock();
        }
    }

    internal virtual void RecordCodeLocation(
        MetricType type,
        string key,
        MeasurementUnit unit,
        int stackLevel,
        DateTimeOffset timestamp
    )
    {
        var startOfDay = timestamp.GetDayBucketKey();
        var metaKey = new MetricResourceIdentifier(type, key, unit);
        var seenToday = _seenLocations.GetOrAdd(startOfDay, _ => []);

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
                    await Task.Delay(_options.ShutdownTimeout, _shutdownSource.Token).ConfigureAwait(false);
                }
                // Cancellation requested and no timeout allowed, so exit even if there are more items
                catch (OperationCanceledException) when (_options.ShutdownTimeout == TimeSpan.Zero)
                {
                    _options.LogDebug(ShutdownImmediatelyMessage);

                    await shutdownTimeout.CancelAsync().ConfigureAwait(false);

                    return;
                }
                // Cancellation requested, scheduled shutdown
                catch (OperationCanceledException)
                {
                    _options.LogDebug(ShutdownScheduledMessage, _options.ShutdownTimeout);

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
                ConcurrentDictionary<string, Metric>? bucket;
                _bucketsLock.EnterWriteLock();
                try
                {
                    if (!Buckets.ContainsKey(key))
                    {
                        continue;
                    }
                    bucket = Buckets[key];
                    Buckets.Remove(key);
                }
                finally
                {
                    _bucketsLock.ExitWriteLock();
                }

                _metricHub.CaptureMetrics(bucket.Values);
                _options.LogDebug("Metric flushed for bucket {0}", key);
            }

            foreach (var (timestamp, locations) in FlushableLocations())
            {
                cancellationToken.ThrowIfCancellationRequested();

                _options.LogDebug("Flushing code locations: ", timestamp);
                var codeLocations = new CodeLocations(timestamp, locations);
                _metricHub.CaptureCodeLocations(codeLocations);
                _options.LogDebug("Code locations flushed: ", timestamp);
            }

            ClearStaleLocations();
        }
        catch (OperationCanceledException)
        {
            _options.LogInfo(FlushShutdownMessage);
        }
        catch (Exception exception)
        {
            _options.LogError(exception, "Error processing metrics.");
        }
        finally
        {
            // If the shutdown token was cancelled before we start this method, we can get here
            // without the _flushLock.CurrentCount (i.e. available threads) having been decremented
            if (_flushLock.CurrentCount < 1)
            {
                _flushLock.Release();
            }
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

        long[] keys;
        _bucketsLock.EnterReadLock();
        try
        {
            keys = Buckets.Keys.ToArray();
        }
        finally
        {
            _bucketsLock.ExitReadLock();
        }
        if (force)
        {
            // Return all the buckets in this case
            foreach (var key in keys)
            {
                yield return key;
            }
        }
        else
        {
            var cutoff = MetricHelper.GetCutoff();
            foreach (var key in keys)
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
    internal void ClearStaleLocations(DateTimeOffset? testNow = null)
    {
        var now = testNow ?? DateTimeOffset.UtcNow;
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
        _options.LogDebug(DisposingMessage);

        if (_disposed)
        {
            _options.LogDebug(AlreadyDisposedMessage);
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
            await _loopTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _options.LogDebug(CancelledMessage);
        }
        catch (Exception exception)
        {
            _options.LogError(exception, "Async Disposing the Metric Aggregator threw an exception.");
        }
        finally
        {
            _flushLock.Dispose();
            _shutdownSource.Dispose();
            _loopTask.Dispose();
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

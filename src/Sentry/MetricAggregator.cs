using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Protocol.Metrics;

namespace Sentry;

internal class MetricAggregator : IMetricAggregator, IDisposable
{
    internal enum MetricType : byte { Counter, Gauge, Distribution, Set }

    private readonly SentryOptions _options;
    private readonly Action<IEnumerable<Metric>> _captureMetrics;
    private readonly TimeSpan _flushInterval;

    private readonly CancellationTokenSource _shutdownSource;
    private volatile bool _disposed;

    // The key for this dictionary is the Timestamp for the bucket, rounded down to the nearest RollupInSeconds... so it
    // aggregates all of the metrics data for a particular time period. The Value is a dictionary for the metrics,
    // each of which has a key that uniquely identifies it within the time period
    internal ConcurrentDictionary<long, ConcurrentDictionary<string, Metric>> Buckets => _buckets.Value;
    private readonly Lazy<ConcurrentDictionary<long, ConcurrentDictionary<string, Metric>>> _buckets
        = new(() => new ConcurrentDictionary<long, ConcurrentDictionary<string, Metric>>());

    private Task LoopTask { get; }

    /// <summary>
    /// MetricAggregator constructor.
    /// </summary>
    /// <param name="options">The <see cref="SentryOptions"/></param>
    /// <param name="captureMetrics">The callback to be called to transmit aggregated metrics to a statsd server</param>
    /// <param name="shutdownSource">A <see cref="CancellationTokenSource"/></param>
    /// <param name="disableLoopTask">
    /// A boolean value indicating whether the Loop to flush metrics should run, for testing only.
    /// </param>
    /// <param name="flushInterval">An optional flushInterval, for testing only</param>
    public MetricAggregator(SentryOptions options, Action<IEnumerable<Metric>> captureMetrics,
        CancellationTokenSource? shutdownSource = null, bool disableLoopTask = false, TimeSpan? flushInterval = null)
    {
        _options = options;
        _captureMetrics = captureMetrics;
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

    internal static string GetMetricBucketKey(MetricType type, string metricKey, MeasurementUnit unit, IDictionary<string, string>? tags)
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
        DateTime? timestamp = null
        // , int stacklevel = 0 // Used for code locations
        ) => Emit(MetricType.Counter, key, value, unit, tags, timestamp);

    /// <inheritdoc cref="IMetricAggregator.Gauge"/>
    public void Gauge(
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTime? timestamp = null
        // , int stacklevel = 0 // Used for code locations
    ) => Emit(MetricType.Gauge, key, value, unit, tags, timestamp);

    /// <inheritdoc cref="IMetricAggregator.Distribution"/>
    public void Distribution(
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTime? timestamp = null
        // , int stacklevel = 0 // Used for code locations
    ) => Emit(MetricType.Distribution, key, value, unit, tags, timestamp);

    /// <inheritdoc cref="IMetricAggregator.Set"/>
    public void Set(
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTime? timestamp = null
        // , int stacklevel = 0 // Used for code locations
    ) => Emit(MetricType.Set, key, value, unit, tags, timestamp);

    /// <inheritdoc cref="IMetricAggregator.Timing"/>
    public void Timing(
        string key,
        double value,
        MeasurementUnit.Duration unit = MeasurementUnit.Duration.Second,
        IDictionary<string, string>? tags = null,
        DateTime? timestamp = null)
        // , int stacklevel = 0 // Used for code locations
        => Emit(MetricType.Distribution, key, value, unit, tags, timestamp);

    private void Emit(
        MetricType type,
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTime? timestamp = null
        // , int stacklevel = 0 // Used for code locations
    )
    {
        timestamp ??= DateTime.UtcNow;
        unit ??= MeasurementUnit.None;
        var timeBucket = Buckets.GetOrAdd(
            timestamp.Value.GetTimeBucketKey(),
            _ => new ConcurrentDictionary<string, Metric>()
        );

        Func<string, Metric> addValuesFactory = type switch
        {
            MetricType.Counter => (string _) => new CounterMetric(key, value, unit.Value, tags, timestamp),
            MetricType.Gauge => (string _) => new GaugeMetric(key, value, unit.Value, tags, timestamp),
            MetricType.Distribution => (string _) => new DistributionMetric(key, value, unit.Value, tags, timestamp),
            MetricType.Set => (string _) => new SetMetric(key, (int)value, unit.Value, tags, timestamp),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown MetricType")
        };

        timeBucket.AddOrUpdate(
            GetMetricBucketKey(type, key, unit.Value, tags),
            addValuesFactory,
            (_, metric) =>
            {
                metric.Add(value);
                return metric;
            }
        );
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

                // Work with the envelope while it's in the queue
                foreach (var key in GetFlushableBuckets())
                {
                    if (shutdownRequested)
                    {
                        break;
                    }
                    try
                    {
                        _options.LogDebug("Flushing metrics for bucket {0}", key);
                        if (Buckets.TryRemove(key, out var bucket))
                        {
                            _captureMetrics(bucket.Values);
                            _options.LogDebug("Metric flushed for bucket {0}", key);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _options.LogInfo("Shutdown token triggered. Time to exit.");
                        return;
                    }
                    catch (Exception exception)
                    {
                        _options.LogError(exception, "Error while processing metric aggregates.");
                    }
                }
            }
        }
        catch (Exception e)
        {
            _options.LogFatal(e, "Exception in the Metric Aggregator.");
            throw;
        }
    }

    /// <summary>
    /// Returns the keys for any buckets that are ready to be flushed (i.e. are for periods before the cutoff)
    /// </summary>
    /// <returns>
    /// An enumerable containing the keys for any buckets that are ready to be flushed
    /// </returns>
    internal IEnumerable<long> GetFlushableBuckets()
    {
        if (!_buckets.IsValueCreated)
        {
            yield break;
        }

        var cutoff = MetricBucketHelper.GetCutoff();
        foreach (var bucket in Buckets)
        {
            var bucketTime = DateTimeOffset.FromUnixTimeSeconds(bucket.Key);
            if (bucketTime < cutoff)
            {
                yield return bucket.Key;
            }
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

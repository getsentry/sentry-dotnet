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

    // TODO: Initialize seen_locations
    // self._seen_locations = _set()  # type: Set[Tuple[int, MetricMetaKey]]
    // self._pending_locations = {}  # type: Dict[int, List[Tuple[MetricMetaKey, Any]]]

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

    private readonly object _emitLock = new();
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

        var metric = timeBucket.GetOrAdd(
            GetMetricBucketKey(type, key, unit.Value, tags),
            _ => AddValues(timestamp.Value));
        lock (_emitLock)
        {
            metric.Add(value);
        }

        // TODO: record the code location
        // if stacklevel is not None:
        //     self.record_code_location(ty, key, unit, stacklevel + 2, timestamp)

        Metric AddValues(DateTime ts) =>
            type switch
            {
                MetricType.Counter => new CounterMetric(key, value, unit.Value, tags, ts),
                MetricType.Gauge => new GaugeMetric(key, value, unit.Value, tags, ts),
                MetricType.Distribution => new DistributionMetric(key, value, unit.Value, tags, ts),
                MetricType.Set => new SetMetric(key, (int)value, unit.Value, tags, ts),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown MetricType")
            };
    }

    // TODO: record_code_location
    // def record_code_location(
    //     self,
    //     ty,  # type: MetricType
    //     key,  # type: str
    //     unit,  # type: MeasurementUnit
    //     stacklevel,  # type: int
    //     timestamp=None,  # type: Optional[float]
    // ):
    //     # type: (...) -> None
    //     if not self._enable_code_locations:
    //         return
    //     if timestamp is None:
    //         timestamp = time.time()
    //     meta_key = (ty, key, unit)
    //     start_of_day = utc_from_timestamp(timestamp).replace(
    //         hour=0, minute=0, second=0, microsecond=0, tzinfo=None
    //     )
    //     start_of_day = int(to_timestamp(start_of_day))
    //
    //     if (start_of_day, meta_key) not in self._seen_locations:
    //         self._seen_locations.add((start_of_day, meta_key))
    //         loc = get_code_location(stacklevel + 3)
    //         if loc is not None:
    //             # Group metadata by day to make flushing more efficient.
    //             # There needs to be one envelope item per timestamp.
    //             self._pending_locations.setdefault(start_of_day, []).append(
    //                 (meta_key, loc)
    //             )

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

                if (shutdownRequested || !Flush())
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
    /// Flushes any flushable buckets.
    /// If <paramref name="force"/> is true then the cutoff is ignored and all buckets are flushed.
    /// </summary>
    /// <param name="force">Forces all buckets to be flushed, ignoring the cutoff</param>
    /// <returns>False if a shutdown is requested during flush, true otherwise</returns>
    private bool Flush(bool force = false)
    {
        // We don't want multiple flushes happening concurrently... which might be possible if the regular flush loop
        // triggered a flush at the same time ForceFlush is called
        lock(_flushLock)
        {
            foreach (var key in GetFlushableBuckets(force))
            {
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
                    return false;
                }
                catch (Exception exception)
                {
                    _options.LogError(exception, "Error while processing metric aggregates.");
                }
            }

            // TODO: Flush the code locations
            // for timestamp, locations in GetFlushableLocations()):
            //     encoded_locations = _encode_locations(timestamp, locations)
            //     envelope.add_item(Item(payload=encoded_locations, type="metric_meta"))
        }

        return true;
    }

    internal bool ForceFlush() => Flush(true);

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
            var cutoff = MetricBucketHelper.GetCutoff();
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

    // TODO: _flushable_locations
    // def _flushable_locations(self):
    //     # type: (...) -> Dict[int, List[Tuple[MetricMetaKey, Dict[str, Any]]]]
    //     with self._lock:
    //         locations = self._pending_locations
    //         self._pending_locations = {}
    //     return locations

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

using Sentry.Internal.Extensions;
using Sentry.Protocol.Metrics;

namespace Sentry;

internal class MetricAggregator
{
    private const int RollupInSeconds = 10;
    private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(5);

    // The key for this dictionary is the Timestamp for the bucket, rounded down to the nearest RollupInSeconds... so it
    // aggregates all of the metrics data for a particular time period. The Value is a dictionary for the metrics,
    // each of which has a key that uniquely identifies it within the time period
    internal ConcurrentDictionary<long, ConcurrentDictionary<string, MetricData>> Buckets => _buckets.Value;
    private readonly Lazy<ConcurrentDictionary<long, ConcurrentDictionary<string, MetricData>>> _buckets
        = new(() => new ConcurrentDictionary<long, ConcurrentDictionary<string, MetricData>>());

    // private readonly Timer _flushTimer;
    // private readonly Action<IEnumerable<Metric>> _onFlush;

    // public MetricsAggregator(TimeSpan? flushInterval, Action<IEnumerable<Metric>> onFlush)
    // {
    //     if (flushInterval.HasValue)
    //     {
    //         _flushInterval = flushInterval.Value;
    //     }
    //     _onFlush = onFlush;
    //     _flushTimer = new Timer(FlushData, null, _flushInterval, _flushInterval);
    // }

    private static readonly DateTime EpochStart = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
    internal static long GetTimeBucketKey(DateTime timestamp)
    {
        var seconds = (long)(timestamp.ToUniversalTime() - EpochStart).TotalSeconds;

        // var seconds = (timestamp?.ToUniversalTime() ??  DateTimeOffset.UtcNow).ToUnixTimeSeconds();
        return (seconds / RollupInSeconds) * RollupInSeconds;
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

    /// <summary>
    /// Emit a counter.
    /// </summary>
    public void Increment(
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTime? timestamp = null
        // , int stacklevel = 0 // Used for code locations
        )
    {
        timestamp ??= DateTime.UtcNow;
        var timeBucket = Buckets.GetOrAdd(
            GetTimeBucketKey(timestamp.Value),
            _ => new ConcurrentDictionary<string, MetricData>()
        );

        timeBucket.AddOrUpdate(
            GetMetricBucketKey(MetricType.Counter, key, unit ?? MeasurementUnit.None, tags),
            _ => new MetricData
            {
                Type = MetricType.Counter,
                Key = key,
                Value = value,
                Unit = unit ?? MeasurementUnit.None,
                Timestamp = timestamp.Value,
                Tags = tags
            },
            (_, metric) =>
            {
                metric.Value += value;
                return metric;
            }
        );
    }

    // // Emit a gauge.
    // public void Gauge(string gaugeName, double value, IDictionary<string, string> tags);
    //
    // // Emit a distribution.
    // public void Distribution(string distributionName, double value, IDictionary<string, string> tags, string? unit = "second");
    //
    // // Emit a set
    // public void Set(string key, string value, IDictionary<string, string> tags);

    // private void FlushData(object? state)
    // {
    //     var metricsToFlush = new List<Metric>();
    //
    //     foreach (var metric in _metrics)
    //     {
    //         metricsToFlush.Add(metric.Value);
    //         // Optionally, reset or remove the metric from _metrics
    //     }
    //
    //     _onFlush(metricsToFlush);
    // }
    //
    // // Method to force flush the data
    // public void ForceFlush()
    // {
    //     FlushData(null);
    // }
    //
    // // Dispose pattern to clean up resources
    // public void Dispose()
    // {
    //     _flushTimer?.Dispose();
    // }

}

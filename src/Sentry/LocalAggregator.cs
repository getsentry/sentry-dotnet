using Sentry.Protocol.Metrics;

namespace Sentry;

internal class LocalAggregator
{
    private Lazy<ConcurrentDictionary<string, GaugeMetric>> LazyMeasurements { get; } = new();
    internal ConcurrentDictionary<string, GaugeMetric> Measurements => LazyMeasurements.Value;

    public void Add(
        MetricType ty,
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null
        )
    {
        timestamp ??= DateTimeOffset.UtcNow;
        unit ??= MeasurementUnit.None;

        // var exportKey = $"{ty}:{key}@{unit}";
        var bucketKey = MetricHelper.GetMetricBucketKey(ty, key, unit.Value, tags);

        Measurements.AddOrUpdate(
            bucketKey,
            _ => new GaugeMetric(key, value, unit.Value, tags, timestamp),
            (_, metric) =>
            {
                // This prevents multiple threads from trying to mutate the metric at the same time. The only other
                // operation performed against metrics is adding one to the bucket (guaranteed to be atomic due to
                // the use of a ConcurrentDictionary for the timeBucket).
                lock (metric)
                {
                    metric.Add(value);
                }
                return metric;
            });
    }

    // def to_json(self):
    //     # type: (...) -> Dict[str, Any]
    //     rv = {}  # type: Any
    //     for (export_key, tags), (
    //         v_min,
    //         v_max,
    //         v_count,
    //         v_sum,
    //     ) in self._measurements.items():
    //         rv.setdefault(export_key, []).append(
    //             {
    //                 "tags": _tags_to_dict(tags),
    //                 "min": v_min,
    //                 "max": v_max,
    //                 "count": v_count,
    //                 "sum": v_sum,
    //             }
    //         )
    //     return rv

}

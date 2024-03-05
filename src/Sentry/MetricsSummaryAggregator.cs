using Sentry.Protocol.Metrics;

namespace Sentry;

internal class MetricsSummaryAggregator
{
    private Lazy<ConcurrentDictionary<string, SpanMetric>> LazyMeasurements { get; } = new();
    internal ConcurrentDictionary<string, SpanMetric> Measurements => LazyMeasurements.Value;

    public void Add(
        MetricType ty,
        string key,
        double value = 1.0,
        MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null
        )
    {
        unit ??= MeasurementUnit.None;

        var bucketKey = MetricHelper.GetMetricBucketKey(ty, key, unit.Value, tags);

        Measurements.AddOrUpdate(
            bucketKey,
            _ => new SpanMetric(ty, key, value, unit.Value, tags),
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
}

namespace Sentry;

internal class DelegatingMetricAggregator(IMetricAggregator innerAggregator) : IMetricAggregator
{
    public void Increment(string key, double value = 1, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null) => innerAggregator.Increment(key, value, unit, tags, timestamp);

    public void Gauge(string key, double value = 1, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null) => innerAggregator.Gauge(key, value, unit, tags, timestamp);

    public void Distribution(string key, double value = 1, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null) => innerAggregator.Distribution(key, value, unit, tags, timestamp);

    public void Set(string key, double value = 1, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null) => innerAggregator.Set(key, value, unit, tags, timestamp);

    public void Timing(string key, double value, MeasurementUnit.Duration unit = MeasurementUnit.Duration.Second, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null) => innerAggregator.Timing(key, value, unit, tags, timestamp);
}

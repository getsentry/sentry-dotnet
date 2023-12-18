namespace Sentry;

internal class DelegatingMetricAggregator(IMetricAggregator innerAggregator) : IMetricAggregator
{
    internal IMetricAggregator InnerAggregator => innerAggregator;

    public void Increment(string key, double value = 1, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null, int stackLevel = 1) => innerAggregator.Increment(key, value, unit, tags, timestamp, stackLevel + 1);

    public void Gauge(string key, double value = 1, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null, int stackLevel = 1) => innerAggregator.Gauge(key, value, unit, tags, timestamp, stackLevel + 1);

    public void Distribution(string key, double value = 1, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null, int stackLevel = 1) => innerAggregator.Distribution(key, value, unit, tags, timestamp, stackLevel + 1);

    public void Set(string key, double value = 1, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null, int stackLevel = 1) => innerAggregator.Set(key, value, unit, tags, timestamp, stackLevel + 1);

    public void Timing(string key, double value, MeasurementUnit.Duration unit = MeasurementUnit.Duration.Second, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null, int stackLevel = 1) => innerAggregator.Timing(key, value, unit, tags, timestamp, stackLevel + 1);

    public Task FlushAsync(bool force = true, CancellationToken cancellationToken = default) =>
        innerAggregator.FlushAsync(force, cancellationToken);

    public void Dispose() => innerAggregator.Dispose();
}

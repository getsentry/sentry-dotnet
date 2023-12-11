namespace Sentry;

internal class DisabledMetricAggregator : IMetricAggregator
{
    public void Increment(string key, double value = 1, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null)
    {
        // No Op
    }

    public void Gauge(string key, double value = 1, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null)
    {
        // No Op
    }

    public void Distribution(string key, double value = 1, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null)
    {
        // No Op
    }

    public void Set(string key, double value = 1, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null)
    {
        // No Op
    }

    public void Timing(string key, double value, MeasurementUnit.Duration unit = MeasurementUnit.Duration.Second, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null)
    {
        // No Op
    }
}

namespace Sentry;

internal class DisabledMetricAggregator : IMetricAggregator
{
    public void Increment(string key, double value = 1, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null, int stackLevel = 1)
    {
        // No Op
    }

    public void Gauge(string key, double value = 1, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null, int stackLevel = 1)
    {
        // No Op
    }

    public void Distribution(string key, double value = 1, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null, int stackLevel = 1)
    {
        // No Op
    }

    public void Set(string key, double value = 1, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null, int stackLevel = 1)
    {
        // No Op
    }

    public void Timing(string key, double value, MeasurementUnit.Duration unit = MeasurementUnit.Duration.Second, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null, int stackLevel = 1)
    {
        // No Op
    }

    public Task FlushAsync(bool force = true, CancellationToken cancellationToken = default)
    {
        // No Op
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        // No Op
    }
}

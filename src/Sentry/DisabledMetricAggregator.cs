namespace Sentry;

internal class DisabledMetricAggregator : IMetricAggregator
{
    public void Increment(string key, double value = 1.0, MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null, int stackLevel = 1)
    {
        // No Op
    }

    public void Gauge(string key, double value = 1.0, MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null, int stackLevel = 1)
    {
        // No Op
    }

    public void Distribution(string key, double value = 1.0, MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null, int stackLevel = 1)
    {
        // No Op
    }

    public void Set(string key, int value, MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null, int stackLevel = 1)
    {
        // No Op
    }

    public void Timing(string key, double value, MeasurementUnit.Duration unit = MeasurementUnit.Duration.Second,
        IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null, int stackLevel = 1)
    {
        // No Op
    }

    private class NoOpDisposable : IDisposable
    {
        public void Dispose()
        {
            // No Op
        }
    }

    public IDisposable StartTimer(string key, MeasurementUnit.Duration unit = MeasurementUnit.Duration.Second,
        IDictionary<string, string>? tags = null,
        int stackLevel = 1)
    {
        // No Op
        return new NoOpDisposable();
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

namespace Sentry;

internal enum MetricType : byte { Counter, Gauge, Distribution, Set }

internal class MetricData
{
    public MetricType Type { get; set; }
    public string Key { get; set; } = string.Empty; // TODO: Replace with constructor
    public double Value { get; set; }
    public MeasurementUnit Unit { get; set; }

    public IDictionary<string, string>? Tags { get; set; }
    public DateTime Timestamp { get; set; }
}

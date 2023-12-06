using Sentry.Extensibility;

namespace Sentry.Protocol.Metrics;

/// <summary>
/// Counters track a value that can only be incremented.
/// </summary>
internal class CounterMetric : Metric
{
    public CounterMetric()
    {
        Value = 0;
    }

    public CounterMetric(string key, double value, MeasurementUnit? unit = null, IDictionary<string,
        string>? tags = null, DateTime? timestamp = null)
        : base(key, unit, tags, timestamp)
    {
        Value = value;
    }

    public double Value { get; private set; }

    protected override string MetricType => "c";

    public override void Add(double value) => Value += value;

    protected override void WriteConcreteProperties(Utf8JsonWriter writer, IDiagnosticLogger? logger) =>
        writer.WriteNumber("value", Value);
}

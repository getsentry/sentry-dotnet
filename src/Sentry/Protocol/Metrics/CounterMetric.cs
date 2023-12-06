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

    /// <summary>
    /// Counters track a value that can only be incremented.
    /// </summary>
    public CounterMetric(string key, double value, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null)
        : base(key, unit, tags)
    {
        Value = value;
    }

    public double Value { get; private set; }

    public override void Add(double value) => Value += value;

    protected override void WriteConcreteProperties(Utf8JsonWriter writer, IDiagnosticLogger? logger) =>
        writer.WriteNumber("value", Value);
}

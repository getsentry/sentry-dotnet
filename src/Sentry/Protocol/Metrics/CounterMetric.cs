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
        string>? tags = null, DateTimeOffset? timestamp = null)
        : base(key, unit, tags, timestamp)
    {
        Value = value;
    }

    public double Value { get; private set; }

    public override void Add(double value) => Value += value;

    protected override void WriteValues(Utf8JsonWriter writer, IDiagnosticLogger? logger) =>
        writer.WriteNumber("value", Value);

    protected override IEnumerable<object> SerializedStatsdValues()
    {
        yield return Value;
    }
}

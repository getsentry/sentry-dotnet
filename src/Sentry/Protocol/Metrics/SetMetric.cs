using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol.Metrics;

/// <summary>
/// Sets track a set of values on which you can perform aggregations such as count_unique.
/// </summary>
internal class SetMetric : Metric
{
    public SetMetric()
    {
        Value = new HashSet<int>();
    }

    public SetMetric(string key, int value, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTime? timestamp = null)
        : base(key, unit, tags, timestamp)
    {
        Value = new HashSet<int>() { value };
    }

    public HashSet<int> Value { get; private set; }

    protected override string MetricType => "s";

    public override void Add(double value)
    {
        Value.Add((int)value);
    }

    protected override void WriteConcreteProperties(Utf8JsonWriter writer, IDiagnosticLogger? logger) =>
        writer.WriteArrayIfNotEmpty("value", Value, logger);
}

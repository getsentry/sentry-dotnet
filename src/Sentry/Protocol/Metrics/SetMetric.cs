using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol.Metrics;

/// <summary>
/// Sets track a set of values on which you can perform aggregations such as count_unique.
/// </summary>
internal class SetMetric : Metric
{
    private readonly HashSet<int> _value;

    public SetMetric()
    {
        _value = new HashSet<int>();
    }

    public SetMetric(string key, int value, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null,
        DateTimeOffset? timestamp = null)
        : base(key, unit, tags, timestamp)
    {
        _value = new HashSet<int>() { value };
    }

    public IReadOnlyCollection<int> Value => _value;

    public override void Add(double value) => _value.Add((int)value);

    protected override void WriteValues(Utf8JsonWriter writer, IDiagnosticLogger? logger) =>
        writer.WriteArrayIfNotEmpty("value", _value, logger);

    protected override IEnumerable<IConvertible> SerializedStatsdValues()
        => _value.Cast<IConvertible>();
}

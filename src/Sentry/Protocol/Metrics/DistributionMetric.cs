using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol.Metrics;

/// <summary>
/// Distributions track a list of values over time in on which you can perform aggregations like max, min, avg.
/// </summary>
internal class DistributionMetric : Metric
{
    private readonly List<double> _value;

    public DistributionMetric()
    {
        _value = new List<double>();
    }

    public DistributionMetric(string key, double value, MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null, DateTimeOffset? timestamp = null)
        : base(key, unit, tags, timestamp)
    {
        _value = new List<double>() { value };
    }

    public IReadOnlyList<double> Value => _value;

    public override void Add(double value) => _value.Add(value);

    protected override void WriteValues(Utf8JsonWriter writer, IDiagnosticLogger? logger) =>
        writer.WriteArrayIfNotEmpty<double>("value", _value, logger);

    protected override IEnumerable<object> SerializedStatsdValues()
        => _value.Cast<object>();
}

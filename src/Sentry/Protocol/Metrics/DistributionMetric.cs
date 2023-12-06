using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol.Metrics;

/// <summary>
/// Distributions track a list of values over time in on which you can perform aggregations like max, min, avg.
/// </summary>
internal class DistributionMetric : Metric
{
    public DistributionMetric()
    {
        Value = new List<double>();
    }

    public DistributionMetric(string key, double value, MeasurementUnit? unit = null,
        IDictionary<string, string>? tags = null, DateTime? timestamp = null)
        : base(key, unit, tags, timestamp)
    {
        Value = new List<double>() { value };
    }

    public IList<double> Value { get; set; }

    protected override string MetricType => "d";

    public override void Add(double value)
    {
        Value.Add(value);
    }

    protected override void WriteConcreteProperties(Utf8JsonWriter writer, IDiagnosticLogger? logger) =>
        writer.WriteArrayIfNotEmpty<double>("value", Value, logger);
}

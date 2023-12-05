using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol.Metrics;

/// <summary>
/// Distributions track a list of values over time in on which you can perform aggregations like max, min, avg.
/// </summary>
internal class DistributionMetric : Metric
{
    IEnumerable<double> Value { get; set; } = new List<double>();

    public override void WriteConcreteProperties(Utf8JsonWriter writer, IDiagnosticLogger? logger) =>
        writer.WriteArrayIfNotEmpty<double>("value", Value, logger);
}

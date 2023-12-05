using Sentry.Extensibility;

namespace Sentry.Protocol.Metrics;

/// <summary>
/// Gauges track a value that can go up and down.
/// </summary>
internal class GaugeMetric : Metric
{
    double Value { get; set; }
    double First { get; set; }
    double Min { get; set; }
    double Max { get; set; }
    double Sum { get; set; }
    double Count { get; set; }

    public override void WriteConcreteProperties(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteNumber("value", Value);
        writer.WriteNumber("first", First);
        writer.WriteNumber("min", Min);
        writer.WriteNumber("max", Max);
        writer.WriteNumber("sum", Sum);
        writer.WriteNumber("count", Count);
    }
}

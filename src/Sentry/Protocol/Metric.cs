using System.Runtime.InteropServices.Marshalling;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

internal abstract class Metric : IJsonSerializable
{
    public string Name { get; set; }
    public DateTime Timestamp { get; set; }
    public MeasurementUnit? Unit { get; set; }

    private readonly Lazy<IDictionary<string, string>> _tags = new(() => new Dictionary<string, string>());
    public IDictionary<string, string> Tags => _tags.Value;

    public abstract void WriteConcreteProperties(Utf8JsonWriter writer, IDiagnosticLogger? logger);

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();
        writer.WriteString("name", Name.ToString());
        writer.WriteString("timestamp", Timestamp);
        if (Unit.HasValue)
        {
            writer.WriteStringIfNotWhiteSpace("unit", Unit.ToString());
        }
        writer.WriteStringDictionaryIfNotEmpty("tags", Tags!);
        WriteConcreteProperties(writer, logger);
        writer.WriteEndObject();
    }
}

/// <summary>
/// Counters track a value that can only be incremented.
/// </summary>
internal class CounterMetric : Metric {
    private int Value { get; set; }

    public override void WriteConcreteProperties(Utf8JsonWriter writer, IDiagnosticLogger? logger) =>
        writer.WriteNumber("value", Value);
}

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

/// <summary>
/// Distributions track a list of values over time in on which you can perform aggregations like max, min, avg.
/// </summary>
internal class DistributionMetric : Metric
{
    IEnumerable<double> Value { get; set; } = new List<double>();

    public override void WriteConcreteProperties(Utf8JsonWriter writer, IDiagnosticLogger? logger) =>
        writer.WriteArrayIfNotEmpty<double>("value", Value, logger);
}

/// <summary>
/// Sets track a set of values on which you can perform aggregations such as count_unique.
/// </summary>
internal class SetMetric : Metric
{
    private HashSet<int> Value { get; set; } = new();

    public override void WriteConcreteProperties(Utf8JsonWriter writer, IDiagnosticLogger? logger) =>
        writer.WriteArrayIfNotEmpty("value", Value, logger);
}

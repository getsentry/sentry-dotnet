using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol.Metrics;

/// <summary>
/// Gauges track a value that can go up and down.
/// </summary>
internal class LocalAggregate
{
    public LocalAggregate(MetricType metricType, string key, double value, MeasurementUnit unit,
        IDictionary<string, string>? tags = null)
    {
        MetricType = metricType;
        Key = key;
        Unit = unit;
        Min = value;
        Max = value;
        Sum = value;
        Count = 1;
        Tags = tags;
    }

    public MetricType MetricType { get; }
    public string Key { get; }
    public MeasurementUnit Unit { get; }
    public double Min { get; private set; }
    public double Max { get; private set; }
    public double Sum { get; private set; }
    public double Count { get; private set; }
    public IDictionary<string, string>? Tags { get; }

    public string ExportKey => $"{MetricType.ToStatsdType()}:{Key}@{Unit}";

    public void Add(double value)
    {
        Min = Math.Min(Min, value);
        Max = Math.Max(Max, value);
        Sum += value;
        Count++;
    }

    /// <inheritdoc cref="ISentryJsonSerializable.WriteTo"/>
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();
        writer.WriteNumber("min", Min);
        writer.WriteNumber("max", Max);
        writer.WriteNumber("count", Count);
        writer.WriteNumber("sum", Sum);
        writer.WriteStringDictionaryIfNotEmpty("tags", (IEnumerable<KeyValuePair<string, string?>>?)Tags);
        writer.WriteEndObject();
    }
}

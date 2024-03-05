using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol.Metrics;

internal record SpanMetric
{
    public SpanMetric(MetricType MetricType,
        string key,
        double value,
        MeasurementUnit unit,
        IDictionary<string, string>? tags = null)
    {
        this.MetricType = MetricType;
        Key = key;
        Unit = unit;
        Tags = tags;
        Min = value;
        Max = value;
        Sum = value;
    }

    public MetricType MetricType { get; init; }
    public string Key { get; init; }
    public MeasurementUnit Unit { get; init; }
    public IDictionary<string, string>? Tags { get; init; }

    public double Min { get; private set; }
    public double Max { get; private set; }
    public double Sum { get; private set; }
    public double Count { get; private set; } = 1;

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

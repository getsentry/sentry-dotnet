using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol.Metrics;

internal abstract class Metric : IJsonSerializable
{
    protected Metric() : this(string.Empty)
    {
    }

    protected Metric(string key, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null, DateTime? timestamp = null)
    {
        Key = key;
        Unit = unit;
        Tags = tags ?? new Dictionary<string, string>();
        Timestamp = timestamp ?? DateTime.UtcNow;
    }

    public string Key { get; private set; }

    public DateTime Timestamp { get; private set; }

    public MeasurementUnit? Unit { get; private set; }

    public IDictionary<string, string> Tags { get; private set; }

    protected abstract string MetricType { get; }

    public abstract void Add(double value);

    protected abstract void WriteConcreteProperties(Utf8JsonWriter writer, IDiagnosticLogger? logger);

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();
        writer.WriteString("type", MetricType);
        writer.WriteString("name", Key);
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

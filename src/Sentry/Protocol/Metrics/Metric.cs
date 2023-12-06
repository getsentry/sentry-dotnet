using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol.Metrics;

internal abstract class Metric : IJsonSerializable
{
    protected Metric() : this(string.Empty)
    {
    }

    protected Metric(string key, MeasurementUnit? unit = null, IDictionary<string, string>? tags = null)
    {
        Key = key;
        Unit = unit;
        Tags = tags ?? new Dictionary<string, string>();
    }

    public string Key { get; private set; }

    public MeasurementUnit? Unit { get; private set; }

    public IDictionary<string, string> Tags { get; private set; }

    public abstract void Add(double value);

    protected abstract void WriteConcreteProperties(Utf8JsonWriter writer, IDiagnosticLogger? logger);

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();
        writer.WriteString("name", Key.ToString());
        if (Unit.HasValue)
        {
            writer.WriteStringIfNotWhiteSpace("unit", Unit.ToString());
        }
        writer.WriteStringDictionaryIfNotEmpty("tags", Tags!);
        WriteConcreteProperties(writer, logger);
        writer.WriteEndObject();
    }
}

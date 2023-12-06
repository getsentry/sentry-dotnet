using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol.Metrics;

internal abstract class Metric : IJsonSerializable
{
    public string Name { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now; // TODO: Replace with constructor
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

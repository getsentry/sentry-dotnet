using Sentry.Extensibility;

namespace Sentry.Protocol;

internal sealed class StructuredLog : ISentryJsonSerializable
{
    private readonly SentryLog[] _items;

    public StructuredLog(SentryLog log)
    {
        _items = [log];
    }

    public StructuredLog(SentryLog[] logs)
    {
        _items = logs;
    }

    public int Length => _items.Length;
    public ReadOnlySpan<SentryLog> Items => _items;

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();
        writer.WriteStartArray("items");

        foreach (var log in _items)
        {
            log.WriteTo(writer, logger);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}

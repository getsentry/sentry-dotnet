using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Protocol;

/// <summary>
/// Represents the Sentry Log protocol.
/// </summary>
/// <remarks>
/// Sentry Docs: <see href="https://docs.sentry.io/product/explore/logs/"/>.
/// Sentry Developer Documentation: <see href="https://develop.sentry.dev/sdk/telemetry/logs/"/>.
/// </remarks>
internal sealed class StructuredLog : ISentryJsonSerializable
{
    private readonly SentryLog[] _items;

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

    internal static void Capture(IHub hub, SentryLog[] logs)
    {
        _ = hub.CaptureEnvelope(Envelope.FromLog(new StructuredLog(logs)));
    }
}

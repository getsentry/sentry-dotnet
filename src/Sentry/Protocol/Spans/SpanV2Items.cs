using Sentry.Extensibility;

namespace Sentry.Protocol.Spans;

/// <summary>
/// Span v2 envelope item payload.
/// </summary>
/// <remarks>
/// Developer docs: https://develop.sentry.dev/sdk/telemetry/spans/span-protocol/
/// </remarks>
internal sealed class SpanV2Items : ISentryJsonSerializable
{
    private readonly IReadOnlyCollection<SpanV2> _spans;

    public SpanV2Items(IReadOnlyCollection<SpanV2> spans)
    {
        _spans = (spans.Count > SpanV2.MaxSpansPerEnvelope)
            ? [.. spans.Take(SpanV2.MaxSpansPerEnvelope)]
            : spans;
    }

    public int Length => _spans.Count;

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();
        writer.WriteStartArray("items");

        foreach (var span in _spans)
        {
            span.WriteTo(writer, logger);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}

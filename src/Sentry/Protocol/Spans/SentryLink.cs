using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol.Spans;

/// <summary>
/// Links connect spans to other spans or traces, enabling distributed tracing
/// </summary>
internal readonly struct SentryLink(SentryId traceId, SpanId spanId, bool sampled) : ISentryJsonSerializable
{
    private readonly SentryAttributes _attributes = new ();

    public SpanId SpanId { get; } = spanId;
    public SentryId TraceId { get; } = traceId;
    public bool Sampled { get; } = sampled;
    public IReadOnlyDictionary<string, SentryAttribute> Attributes => _attributes;

    /// <inheritdoc cref="ISentryJsonSerializable.WriteTo(Utf8JsonWriter, IDiagnosticLogger)" />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteSerializableIfNotNull("span_id", SpanId.NullIfDefault(), logger);
        writer.WriteSerializableIfNotNull("trace_id", TraceId.NullIfDefault(), logger);
        writer.WriteBoolean("sampled", Sampled);

        _attributes.WriteTo(writer, logger);

        writer.WriteEndObject();
    }
}

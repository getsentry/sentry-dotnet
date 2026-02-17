using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol.Spans;

/// <summary>
/// Represents a single Span (Span v2 protocol) to be sent in a dedicated span envelope item.
/// </summary>
/// <remarks>
/// Developer docs: https://develop.sentry.dev/sdk/telemetry/spans/span-protocol/
/// </remarks>
internal sealed class SpanV2 : ISentryJsonSerializable
{
    public const int MaxSpansPerEnvelope = 1000;

    private readonly SentryAttributes _attributes = new();

    public SpanV2(
        SentryId traceId,
        SpanId spanId,
        string name,
        DateTimeOffset startTimestamp)
    {
        TraceId = traceId;
        SpanId = spanId;
        Name = name;
        StartTimestamp = startTimestamp;
    }

    /// <summary>
    /// Converts a <see cref="SentryTransaction"/> to a <see cref="SpanV2"/>.
    /// </summary>
    /// <remarks>This is a temporary method. We can remove it once transactions have been deprecated</remarks>
    internal SpanV2(SentryTransaction transaction) : this(transaction.TraceId, transaction.SpanId,
        transaction.Name, transaction.StartTimestamp)
    {
        ParentSpanId = transaction.ParentSpanId;
        EndTimestamp = transaction.EndTimestamp;
        Status = transaction.Status is SpanStatus.Ok ? SpanV2Status.Ok : SpanV2Status.Error;
        Attributes.SetAttribute(SpanV2Attributes.Operation, transaction.Operation);
        if (transaction.Origin is { } origin)
        {
            Attributes.SetAttribute(SpanV2Attributes.Source, origin);
        }
        foreach (var tag in transaction.Tags)
        {
            Attributes.SetAttribute(tag.Key, tag.Value);
        }

        foreach (var data in transaction.Data)
        {
            if (data.Value is not null)
            {
                Attributes.SetAttribute(data.Key, data.Value);
            }
        }
    }

    /// <summary>
    /// Converts a <see cref="SentrySpan"/> to a <see cref="SpanV2"/>.
    /// </summary>
    /// <remarks>This is a temporary method. We can remove it once transactions have been deprecated</remarks>
    internal SpanV2(SentrySpan span) : this(span.TraceId, span.SpanId, span.Description ?? span.Operation, span.StartTimestamp)
    {
        ParentSpanId = span.ParentSpanId;
        EndTimestamp = span.EndTimestamp;
        Status = span.Status is SpanStatus.Ok ? SpanV2Status.Ok : SpanV2Status.Error;
        Attributes.SetAttribute(SpanV2Attributes.Operation, span.Operation);
        if (span.Origin is { } origin)
        {
            Attributes.SetAttribute(SpanV2Attributes.Source, origin);
        }
        foreach (var tag in span.Tags)
        {
            Attributes.SetAttribute(tag.Key, tag.Value);
        }

        foreach (var data in span.Data)
        {
            if (data.Value is not null)
            {
                Attributes.SetAttribute(data.Key, data.Value);
            }
        }
    }

    public SentryId TraceId { get; }
    public SpanId SpanId { get; }
    public SpanId? ParentSpanId { get; set; }
    public string Name { get; set; }
    public SpanV2Status Status { get; set; }
    public bool IsSegment { get; set; }
    public DateTimeOffset StartTimestamp { get; }
    public DateTimeOffset? EndTimestamp { get; set; }

    public SentryAttributes Attributes => _attributes;
    public List<SentryLink> Links { get; } = [];

    // TODO: Attachments - see https://develop.sentry.dev/sdk/telemetry/spans/span-protocol/#span-attachments

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteSerializable("trace_id", TraceId, logger);
        writer.WriteSerializable("span_id", SpanId, logger);
        writer.WriteSerializableIfNotNull("parent_span_id", ParentSpanId, logger);
        writer.WriteStringIfNotWhiteSpace("status", Status.ToString().ToSnakeCase());
        writer.WriteString("start_timestamp", StartTimestamp);
        writer.WriteStringIfNotNull("timestamp", EndTimestamp);

        _attributes.WriteTo(writer, logger);

        writer.WriteStartArray("links");
        foreach (var link in Links)
        {
            link.WriteTo(writer, logger);
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}

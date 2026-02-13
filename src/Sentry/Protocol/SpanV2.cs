using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Protocol.Metrics;

namespace Sentry.Protocol;

/// <summary>
/// Represents a single Span (Span v2 protocol) to be sent in a dedicated span envelope item.
/// </summary>
/// <remarks>
/// Developer docs: https://develop.sentry.dev/sdk/telemetry/spans/span-protocol/
/// </remarks>
internal sealed class SpanV2 : ISentryJsonSerializable
{
    public const int MaxSpansPerEnvelope = 100;

    public SpanV2(
        SentryId traceId,
        SpanId spanId,
        string operation,
        DateTimeOffset startTimestamp)
    {
        TraceId = traceId;
        SpanId = spanId;
        Operation = operation;
        StartTimestamp = startTimestamp;
    }

    public SentryId TraceId { get; }
    public SpanId SpanId { get; }
    public SpanId? ParentSpanId { get; set; }

    /// <summary>
    /// The span operation.
    /// </summary>
    public string Operation { get; set; }

    public string? Description { get; set; }
    public SpanStatus? Status { get; set; }

    public DateTimeOffset StartTimestamp { get; }
    public DateTimeOffset? EndTimestamp { get; set; }

    public string? Origin { get; set; }

    public string? SegmentId { get; set; }

    public bool? IsSampled { get; set; }

    private Dictionary<string, string>? _tags;
    public IReadOnlyDictionary<string, string> Tags => _tags ??= new Dictionary<string, string>();

    private Dictionary<string, object?>? _data;
    public IReadOnlyDictionary<string, object?> Data => _data ??= new Dictionary<string, object?>();

    private Dictionary<string, Measurement>? _measurements;
    public IReadOnlyDictionary<string, Measurement> Measurements => _measurements ??= new Dictionary<string, Measurement>();

    private MetricsSummary? _metricsSummary;

    public static SpanV2 FromSpan(ISpan span) => new(span.TraceId, span.SpanId, span.Operation, span.StartTimestamp)
    {
        ParentSpanId = span.ParentSpanId,
        Description = span.Description,
        Status = span.Status,
        EndTimestamp = span.EndTimestamp,
        Origin = span.Origin,
        IsSampled = span.IsSampled,
        SegmentId = null, // reserved for future SDK behavior
        _tags = span.Tags.ToDict(),
        _data = span.Data.ToDict(),
        _measurements = span.Measurements.ToDict(),
    };

    public void SetTag(string key, string value) => (_tags ??= new Dictionary<string, string>())[key] = value;
    public void SetData(string key, object? value) => (_data ??= new Dictionary<string, object?>())[key] = value;
    public void SetMeasurement(string name, Measurement measurement) => (_measurements ??= new Dictionary<string, Measurement>())[name] = measurement;
    internal void SetMetricsSummary(MetricsSummary summary) => _metricsSummary = summary;

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteSerializable("trace_id", TraceId, logger);
        writer.WriteSerializable("span_id", SpanId, logger);
        writer.WriteSerializableIfNotNull("parent_span_id", ParentSpanId, logger);

        writer.WriteStringIfNotWhiteSpace("op", Operation);
        writer.WriteStringIfNotWhiteSpace("description", Description);
        writer.WriteStringIfNotWhiteSpace("status", Status?.ToString().ToSnakeCase());

        // Span v2 uses the same timestamp format as other payloads in this SDK.
        writer.WriteString("start_timestamp", StartTimestamp);
        writer.WriteStringIfNotNull("timestamp", EndTimestamp);

        writer.WriteStringIfNotWhiteSpace("origin", Origin);
        writer.WriteStringIfNotWhiteSpace("segment_id", SegmentId);

        if (IsSampled is { } sampled)
        {
            writer.WriteBoolean("sampled", sampled);
        }

        writer.WriteStringDictionaryIfNotEmpty("tags", _tags!);
        writer.WriteDictionaryIfNotEmpty("data", _data!, logger);
        writer.WriteDictionaryIfNotEmpty("measurements", _measurements, logger);
        writer.WriteSerializableIfNotNull("_metrics_summary", _metricsSummary, logger);

        writer.WriteEndObject();
    }
}

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

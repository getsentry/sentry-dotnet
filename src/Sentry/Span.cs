using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Protocol;

namespace Sentry;

// https://develop.sentry.dev/sdk/event-payloads/span
/// <summary>
/// Transaction span.
/// </summary>
public class Span : ISpanData, IJsonSerializable
{
    /// <inheritdoc />
    public SpanId SpanId { get; private set; }

    /// <inheritdoc />
    public SpanId? ParentSpanId { get; private set; }

    /// <inheritdoc />
    public SentryId TraceId { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset StartTimestamp { get; private set; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateTimeOffset? EndTimestamp { get; private set; }

    /// <inheritdoc />
    public bool IsFinished => EndTimestamp is not null;

    // Not readonly because of deserialization
    private Dictionary<string, Measurement> _measurements = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, Measurement> Measurements => _measurements;

    /// <inheritdoc />
    public void SetMeasurement(string name, Measurement measurement) =>
        _measurements[name] = measurement;

    /// <inheritdoc />
    public string Operation { get; set; }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public SpanStatus? Status { get; set; }

    /// <inheritdoc />
    public bool? IsSampled { get; internal set; }

    private Dictionary<string, string>? _tags;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Tags => _tags ??= new Dictionary<string, string>();

    /// <inheritdoc />
    public void SetTag(string key, string value) =>
        (_tags ??= new Dictionary<string, string>())[key] = value;

    /// <inheritdoc />
    public void UnsetTag(string key) =>
        (_tags ??= new Dictionary<string, string>()).Remove(key);

    // Aka 'data'
    private Dictionary<string, object?>? _extra;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Extra => _extra ??= new Dictionary<string, object?>();

    /// <inheritdoc />
    public void SetExtra(string key, object? value) =>
        (_extra ??= new Dictionary<string, object?>())[key] = value;

    /// <summary>
    /// Initializes an instance of <see cref="Span"/>.
    /// </summary>
    public Span(SpanId? parentSpanId, string operation)
    {
        SpanId = SpanId.Create();
        ParentSpanId = parentSpanId;
        TraceId = SentryId.Create();
        Operation = operation;
    }

    /// <summary>
    /// Initializes an instance of <see cref="Span"/>.
    /// </summary>
    public Span(ISpan tracer)
        : this(tracer.ParentSpanId, tracer.Operation)
    {
        SpanId = tracer.SpanId;
        TraceId = tracer.TraceId;
        StartTimestamp = tracer.StartTimestamp;
        EndTimestamp = tracer.EndTimestamp;
        Description = tracer.Description;
        Status = tracer.Status;
        IsSampled = tracer.IsSampled;
        _extra = tracer.Extra.ToDictionary();
        _tags = tracer.Tags.ToDictionary();
        _measurements = tracer.Measurements.ToDictionary();
    }

    /// <inheritdoc />
    public SentryTraceHeader GetTraceHeader() => new(
        TraceId,
        SpanId,
        IsSampled);

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteSerializable("span_id", SpanId, logger);
        writer.WriteSerializableIfNotNull("parent_span_id", ParentSpanId, logger);
        writer.WriteSerializable("trace_id", TraceId, logger);
        writer.WriteStringIfNotWhiteSpace("op", Operation);
        writer.WriteStringIfNotWhiteSpace("description", Description);
        writer.WriteStringIfNotWhiteSpace("status", Status?.ToString().ToSnakeCase());
        writer.WriteString("start_timestamp", StartTimestamp);
        writer.WriteStringIfNotNull("timestamp", EndTimestamp);
        writer.WriteStringDictionaryIfNotEmpty("tags", _tags!);
        writer.WriteDictionaryIfNotEmpty("data", _extra!, logger);
        writer.WriteDictionaryIfNotEmpty("measurements", _measurements, logger);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses a span from JSON.
    /// </summary>
    public static Span FromJson(JsonElement json)
    {
        var spanId = json.GetPropertyOrNull("span_id")?.Pipe(SpanId.FromJson) ?? SpanId.Empty;
        var parentSpanId = json.GetPropertyOrNull("parent_span_id")?.Pipe(SpanId.FromJson);
        var traceId = json.GetPropertyOrNull("trace_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
        var startTimestamp = json.GetProperty("start_timestamp").GetDateTimeOffset();
        var endTimestamp = json.GetProperty("timestamp").GetDateTimeOffset();
        var operation = json.GetPropertyOrNull("op")?.GetString() ?? "unknown";
        var description = json.GetPropertyOrNull("description")?.GetString();
        var status = json.GetPropertyOrNull("status")?.GetString()?.Replace("_", "").ParseEnum<SpanStatus>();
        var isSampled = json.GetPropertyOrNull("sampled")?.GetBoolean();
        var tags = json.GetPropertyOrNull("tags")?.GetStringDictionaryOrNull()?.ToDictionary();
        var measurements = json.GetPropertyOrNull("measurements")?
            .GetDictionaryOrNull(Measurement.FromJson) ?? new();
        var data = json.GetPropertyOrNull("data")?.GetDictionaryOrNull()?.ToDictionary();

        return new Span(parentSpanId, operation)
        {
            SpanId = spanId,
            TraceId = traceId,
            StartTimestamp = startTimestamp,
            EndTimestamp = endTimestamp,
            Description = description,
            Status = status,
            IsSampled = isSampled,
            _tags = tags!,
            _extra = data!,
            _measurements = measurements
        };
    }

    internal void Redact()
    {
        Description = Description?.RedactUrl();
    }
}

using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

/// <summary>
/// Trace context data.
/// </summary>
public class Trace : ITraceContext, ITraceContextInternal, ISentryJsonSerializable, ICloneable<Trace>, IUpdatable<Trace>
{
    /// <summary>
    /// Tells Sentry which type of context this is.
    /// </summary>
    public const string Type = "trace";

    /// <inheritdoc />
    public SpanId SpanId { get; set; }

    /// <inheritdoc />
    public SpanId? ParentSpanId { get; set; }

    /// <inheritdoc />
    public SentryId TraceId { get; set; }

    /// <inheritdoc />
    public string Operation { get; set; } = "";

    /// <inheritdoc />
    public Origin? Origin { get; set; }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public SpanStatus? Status { get; set; }

    /// <inheritdoc />
    public bool? IsSampled { get; internal set; }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    internal Trace Clone() => ((ICloneable<Trace>)this).Clone();

    Trace ICloneable<Trace>.Clone() => new()
    {
        SpanId = SpanId,
        ParentSpanId = ParentSpanId,
        TraceId = TraceId,
        Operation = Operation,
        Status = Status,
        IsSampled = IsSampled
    };

    /// <summary>
    /// Updates this instance with data from the properties in the <paramref name="source"/>,
    /// unless there is already a value in the existing property.
    /// </summary>
    internal void UpdateFrom(Trace source) => ((IUpdatable<Trace>)this).UpdateFrom(source);

    void IUpdatable.UpdateFrom(object source)
    {
        if (source is Trace trace)
        {
            ((IUpdatable<Trace>)this).UpdateFrom(trace);
        }
    }

    void IUpdatable<Trace>.UpdateFrom(Trace source)
    {
        SpanId = SpanId == default ? source.SpanId : SpanId;
        ParentSpanId ??= source.ParentSpanId;
        TraceId = TraceId == default ? source.TraceId : TraceId;
        Operation = string.IsNullOrWhiteSpace(Operation) ? source.Operation : Operation;
        Status ??= source.Status;
        IsSampled ??= source.IsSampled;
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteString("type", Type);
        writer.WriteSerializableIfNotNull("span_id", SpanId.NullIfDefault(), logger);
        writer.WriteSerializableIfNotNull("parent_span_id", ParentSpanId?.NullIfDefault(), logger);
        writer.WriteSerializableIfNotNull("trace_id", TraceId.NullIfDefault(), logger);
        writer.WriteStringIfNotWhiteSpace("op", Operation);
        writer.WriteStringIfNotWhiteSpace("description", Description);
        writer.WriteStringIfNotWhiteSpace("status", Status?.ToString().ToSnakeCase());

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses trace context from JSON.
    /// </summary>
    public static Trace FromJson(JsonElement json)
    {
        var spanId = json.GetPropertyOrNull("span_id")?.Pipe(SpanId.FromJson) ?? SpanId.Empty;
        var parentSpanId = json.GetPropertyOrNull("parent_span_id")?.Pipe(SpanId.FromJson);
        var traceId = json.GetPropertyOrNull("trace_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
        var operation = json.GetPropertyOrNull("op")?.GetString() ?? "";
        var description = json.GetPropertyOrNull("description")?.GetString();
        var status = json.GetPropertyOrNull("status")?.GetString()?.Replace("_", "").ParseEnum<SpanStatus>();
        var isSampled = json.GetPropertyOrNull("sampled")?.GetBoolean();

        return new Trace
        {
            SpanId = spanId,
            ParentSpanId = parentSpanId,
            TraceId = traceId,
            Operation = operation,
            Description = description,
            Status = status,
            IsSampled = isSampled
        };
    }
}

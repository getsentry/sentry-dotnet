using System.Text.Json;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol
{
    /// <summary>
    /// Trace context data.
    /// </summary>
    public class Trace : ITraceContext, IJsonSerializable
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
        public string? Description { get; set; }

        /// <inheritdoc />
        public SpanStatus? Status { get; set; }

        /// <inheritdoc />
        public bool? IsSampled { get; internal set; }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        internal Trace Clone() => new()
        {
            SpanId = SpanId,
            ParentSpanId = ParentSpanId,
            TraceId = TraceId,
            Operation = Operation,
            Status = Status,
            IsSampled = IsSampled
        };

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
}

using System.Text.Json;
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
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteString("type", Type);

            if (SpanId != SpanId.Empty)
            {
                writer.WriteSerializable("span_id", SpanId);
            }

            if (ParentSpanId is {} parentSpanId && parentSpanId != SpanId.Empty)
            {
                writer.WriteSerializable("parent_span_id", parentSpanId);
            }

            if (TraceId != SentryId.Empty)
            {
                writer.WriteSerializable("trace_id", TraceId);
            }

            if (!string.IsNullOrWhiteSpace(Operation))
            {
                writer.WriteString("op", Operation);
            }

            if (Status is {} status)
            {
                writer.WriteString("status", status.ToString().ToSnakeCase());
            }

            if (IsSampled is {} isSampled)
            {
                writer.WriteBoolean("sampled", isSampled);
            }

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
            var status = json.GetPropertyOrNull("status")?.GetString()?.Pipe(s => s.Replace("_", "").ParseEnum<SpanStatus>());
            var isSampled = json.GetPropertyOrNull("sampled")?.GetBoolean();

            return new Trace
            {
                SpanId = spanId,
                ParentSpanId = parentSpanId,
                TraceId = traceId,
                Operation = operation,
                Status = status,
                IsSampled = isSampled
            };
        }
    }
}

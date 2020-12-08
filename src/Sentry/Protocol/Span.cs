using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol
{
    // https://develop.sentry.dev/sdk/event-payloads/span
    /// <summary>
    /// Transaction span.
    /// </summary>
    public class Span : ISpan, IJsonSerializable
    {
        /// <inheritdoc />
        public SentryId SpanId { get; }

        /// <inheritdoc />
        public SentryId? ParentSpanId { get; }

        /// <inheritdoc />
        public SentryId TraceId { get; private set; }

        /// <inheritdoc />
        public DateTimeOffset StartTimestamp { get; private set; }

        /// <inheritdoc />
        public DateTimeOffset? EndTimestamp { get; private set; }

        /// <inheritdoc />
        public string Operation { get; }

        /// <inheritdoc />
        public string? Description { get; set; }

        /// <inheritdoc />
        public SpanStatus? Status { get; private set; }

        /// <inheritdoc />
        public bool IsSampled { get; set; }

        private ConcurrentDictionary<string, string>? _tags;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Tags => _tags ??= new ConcurrentDictionary<string, string>();

        private ConcurrentDictionary<string, object>? _data;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Data => _data ??= new ConcurrentDictionary<string, object>();

        internal Span(SentryId? spanId = null, SentryId? parentSpanId = null, string operation = "unknown")
        {
            SpanId = spanId ?? SentryId.Create();
            ParentSpanId = parentSpanId;
            TraceId = SentryId.Create();
            StartTimestamp = DateTimeOffset.Now;
            Operation = operation;
        }

        /// <inheritdoc />
        public ISpan StartChild(string operation) => new Span(null, SpanId, operation);

        /// <inheritdoc />
        public void Finish(SpanStatus status = SpanStatus.Ok)
        {
            EndTimestamp = DateTimeOffset.Now;
            Status = status;
        }

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteString("type", "transaction");
            writer.WriteString("event_id", SentryId.Create().ToString());

            writer.WriteString("start_timestamp", StartTimestamp);

            if (EndTimestamp is {} endTimestamp)
            {
                writer.WriteString("timestamp", endTimestamp);
            }

            if (_tags is {} tags && tags.Any())
            {
                writer.WriteDictionary("tags", tags!);
            }

            if (_data is {} data && data.Any())
            {
                writer.WriteDictionary("data", data!);
            }

            writer.WriteStartObject("contexts");
            writer.WriteStartObject("trace");

            writer.WriteString("span_id", SpanId.ToShortString());

            if (ParentSpanId is {} parentSpanId)
            {
                writer.WriteString("parent_span_id", parentSpanId.ToShortString());
            }

            writer.WriteSerializable("trace_id", TraceId);

            if (!string.IsNullOrWhiteSpace(Operation))
            {
                writer.WriteString("op", Operation);
            }

            if (!string.IsNullOrWhiteSpace(Description))
            {
                writer.WriteString("description", Description);
            }

            if (Status is {} status)
            {
                writer.WriteString("status", status.ToString().ToLowerInvariant());
            }

            writer.WriteBoolean("sampled", IsSampled);

            writer.WriteEndObject();
            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses a span from JSON.
        /// </summary>
        public static Span FromJson(JsonElement json)
        {
            var spanId = json.GetPropertyOrNull("span_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
            var parentSpanId = json.GetPropertyOrNull("parent_span_id")?.Pipe(SentryId.FromJson);
            var traceId = json.GetPropertyOrNull("trace_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
            var startTimestamp = json.GetProperty("start_timestamp").GetDateTimeOffset();
            var endTimestamp = json.GetProperty("timestamp").GetDateTimeOffset();
            var operation = json.GetPropertyOrNull("op")?.GetString() ?? "unknown";
            var description = json.GetPropertyOrNull("description")?.GetString();
            var status = json.GetPropertyOrNull("status")?.GetString()?.Pipe(s => s.ParseEnum<SpanStatus>());
            var sampled = json.GetPropertyOrNull("sampled")?.GetBoolean() ?? false;
            var tags = json.GetPropertyOrNull("tags")?.GetDictionary()?.Pipe(v => new ConcurrentDictionary<string, string>(v!));
            var data = json.GetPropertyOrNull("data")?.GetObjectDictionary()?.Pipe(v => new ConcurrentDictionary<string, object>(v!));

            return new Span(spanId, parentSpanId, operation)
            {
                TraceId = traceId,
                StartTimestamp = startTimestamp,
                EndTimestamp = endTimestamp,
                Description = description,
                Status = status,
                IsSampled = sampled,
                _tags = tags,
                _data = data
            };
        }
    }
}

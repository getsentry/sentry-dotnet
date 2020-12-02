using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol
{
    // https://develop.sentry.dev/sdk/event-payloads/span
    public class Span : ISpan, IJsonSerializable
    {
        public SentryId SpanId { get; }
        public SentryId? ParentSpanId { get; }
        public SentryId TraceId { get; set; }
        public DateTimeOffset StartTimestamp { get; set; }
        public DateTimeOffset EndTimestamp { get; set; }
        public string Operation { get; }
        public string? Description { get; set; }
        public SpanStatus? Status { get; set; }
        public bool IsSampled { get; set; }

        private ConcurrentDictionary<string, string>? _tags;
        public IReadOnlyDictionary<string, string> Tags => _tags ??= new ConcurrentDictionary<string, string>();

        private ConcurrentDictionary<string, object>? _data;
        public IReadOnlyDictionary<string, object> Data => _data ??= new ConcurrentDictionary<string, object>();

        internal Span(SentryId? spanId = null, SentryId? parentSpanId = null, string operation = "unknown")
        {
            SpanId = spanId ?? SentryId.Create();
            ParentSpanId = parentSpanId;
            Operation = operation;
            StartTimestamp = EndTimestamp = DateTimeOffset.Now;
        }

        public ISpan StartChild(string operation) => new Span(null, SpanId, operation);

        public void Finish()
        {
            EndTimestamp = DateTimeOffset.Now;
        }

        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteString("span_id", SpanId);

            if (ParentSpanId is {} parentSpanId)
            {
                writer.WriteString("parent_span_id", parentSpanId);
            }

            writer.WriteString("trace_id", TraceId);
            writer.WriteString("start_timestamp", StartTimestamp);
            writer.WriteString("timestamp", EndTimestamp);

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

            if (_tags is {} tags && tags.Any())
            {
                writer.WriteDictionary("tags", tags!);
            }

            if (_data is {} data && data.Any())
            {
                writer.WriteDictionary("data", data!);
            }

            writer.WriteEndObject();
        }

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

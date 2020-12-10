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
        private readonly SpanRecorder _parentSpanRecorder;

        /// <inheritdoc />
        public SentryId SpanId { get; }

        /// <inheritdoc />
        public SentryId? ParentSpanId { get; }

        /// <inheritdoc />
        public SentryId TraceId { get; private set; }

        /// <inheritdoc />
        public DateTimeOffset StartTimestamp { get; private set; } = DateTimeOffset.UtcNow;

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

        private ConcurrentDictionary<string, object?>? _data;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object?> Extra => _data ??= new ConcurrentDictionary<string, object?>();

        internal Span(SpanRecorder parentSpanRecorder, SentryId? spanId = null, SentryId? parentSpanId = null, string operation = "unknown")
        {
            _parentSpanRecorder = parentSpanRecorder;
            SpanId = spanId ?? SentryId.Create();
            ParentSpanId = parentSpanId;
            TraceId = SentryId.Create();
            Operation = operation;
        }

        /// <inheritdoc />
        public ISpan StartChild(string operation)
        {
            var span = new Span(_parentSpanRecorder, null, SpanId, operation);
            _parentSpanRecorder.Add(span);

            return span;
        }

        /// <inheritdoc />
        public void Finish(SpanStatus status = SpanStatus.Ok)
        {
            EndTimestamp = DateTimeOffset.UtcNow;
            Status = status;
        }

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

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
                writer.WriteString("status", status.ToString().ToSnakeCase());
            }

            writer.WriteBoolean("sampled", IsSampled);

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

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses a span from JSON.
        /// </summary>
        public static Span FromJson(JsonElement json)
        {
            // TODO
            var parentSpanRecorder = new SpanRecorder();

            var spanId = json.GetPropertyOrNull("span_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
            var parentSpanId = json.GetPropertyOrNull("parent_span_id")?.Pipe(SentryId.FromJson);
            var traceId = json.GetPropertyOrNull("trace_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
            var startTimestamp = json.GetProperty("start_timestamp").GetDateTimeOffset();
            var endTimestamp = json.GetProperty("timestamp").GetDateTimeOffset();
            var operation = json.GetPropertyOrNull("op")?.GetString() ?? "unknown";
            var description = json.GetPropertyOrNull("description")?.GetString();
            var status = json.GetPropertyOrNull("status")?.GetString()?.Pipe(s => s.Replace("_", "").ParseEnum<SpanStatus>());
            var isSampled = json.GetPropertyOrNull("sampled")?.GetBoolean() ?? false;
            var tags = json.GetPropertyOrNull("tags")?.GetDictionary()?.Pipe(v => new ConcurrentDictionary<string, string>(v!));
            var data = json.GetPropertyOrNull("data")?.GetObjectDictionary()?.Pipe(v => new ConcurrentDictionary<string, object?>(v!));

            return new Span(parentSpanRecorder, spanId, parentSpanId, operation)
            {
                TraceId = traceId,
                StartTimestamp = startTimestamp,
                EndTimestamp = endTimestamp,
                Description = description,
                Status = status,
                IsSampled = isSampled,
                _tags = tags,
                _data = data
            };
        }
    }
}

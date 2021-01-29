using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry
{
    // https://develop.sentry.dev/sdk/event-payloads/span
    /// <summary>
    /// Transaction span.
    /// </summary>
    public class Span : ISpan, IJsonSerializable
    {
        // This needs to be Transaction and not ITransaction because
        // ITransaction doesn't contain `StartChild(..., parentSpanId)`
        // which we need here.
        private readonly Transaction _parentTransaction;

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

        /// <inheritdoc cref="ISpan.Operation" />
        public string Operation { get; set; }

        /// <inheritdoc cref="ISpan.Description" />
        public string? Description { get; set; }

        /// <inheritdoc cref="ISpan.Status" />
        public SpanStatus? Status { get; set; }

        /// <inheritdoc />
        public bool? IsSampled { get; internal set; }

        private ConcurrentDictionary<string, string>? _tags;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Tags => _tags ??= new ConcurrentDictionary<string, string>();

        /// <inheritdoc />
        public void SetTag(string key, string value) =>
            (_tags ??= new ConcurrentDictionary<string, string>())[key] = value;

        /// <inheritdoc />
        public void UnsetTag(string key) =>
            (_tags ??= new ConcurrentDictionary<string, string>()).TryRemove(key, out _);

        private ConcurrentDictionary<string, object?>? _data;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object?> Extra => _data ??= new ConcurrentDictionary<string, object?>();

        /// <inheritdoc />
        public void SetExtra(string key, object? value) =>
            (_data ??= new ConcurrentDictionary<string, object?>())[key] = value;

        /// <summary>
        /// Initializes an instance of <see cref="Span"/>.
        /// </summary>
        public Span(Transaction parentTransaction, SpanId? parentSpanId, string operation)
        {
            _parentTransaction = parentTransaction;
            SpanId = SpanId.Create();
            ParentSpanId = parentSpanId;
            TraceId = SentryId.Create();
            Operation = operation;
        }

        /// <inheritdoc />
        public ISpan StartChild(string operation) =>
            _parentTransaction.StartChild(SpanId, operation);

        /// <inheritdoc />
        public void Finish() => EndTimestamp = DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public SentryTraceHeader GetTraceHeader() => new(
            TraceId,
            SpanId,
            IsSampled
        );

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteSerializable("span_id", SpanId);

            if (ParentSpanId is {} parentSpanId)
            {
                writer.WriteSerializable("parent_span_id", parentSpanId);
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

            if (IsSampled is {} isSampled)
            {
                writer.WriteBoolean("sampled", isSampled);
            }

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
        public static Span FromJson(Transaction parentTransaction, JsonElement json)
        {
            var spanId = json.GetPropertyOrNull("span_id")?.Pipe(SpanId.FromJson) ?? SpanId.Empty;
            var parentSpanId = json.GetPropertyOrNull("parent_span_id")?.Pipe(SpanId.FromJson);
            var traceId = json.GetPropertyOrNull("trace_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
            var startTimestamp = json.GetProperty("start_timestamp").GetDateTimeOffset();
            var endTimestamp = json.GetProperty("timestamp").GetDateTimeOffset();
            var operation = json.GetPropertyOrNull("op")?.GetString() ?? "unknown";
            var description = json.GetPropertyOrNull("description")?.GetString();
            var status = json.GetPropertyOrNull("status")?.GetString()?.Pipe(s => s.Replace("_", "").ParseEnum<SpanStatus>());
            var isSampled = json.GetPropertyOrNull("sampled")?.GetBoolean();
            var tags = json.GetPropertyOrNull("tags")?.GetDictionary()?.Pipe(v => new ConcurrentDictionary<string, string>(v!));
            var data = json.GetPropertyOrNull("data")?.GetObjectDictionary()?.Pipe(v => new ConcurrentDictionary<string, object?>(v!));

            return new Span(parentTransaction, parentSpanId, operation)
            {
                SpanId = spanId,
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

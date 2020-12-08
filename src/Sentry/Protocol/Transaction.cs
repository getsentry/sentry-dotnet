using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol
{
    // https://develop.sentry.dev/sdk/event-payloads/transaction
    /// <summary>
    /// Sentry performance transaction.
    /// </summary>
    public class Transaction : ISpan, IJsonSerializable
    {
        private readonly IHub _hub;

        /// <summary>
        /// Transaction name.
        /// </summary>
        public string Name { get; }

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

        private Dictionary<string, string>? _tags;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Tags => _tags ??= new Dictionary<string, string>();

        private Dictionary<string, object>? _data;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object> Data => _data ??= new Dictionary<string, object>();

        private List<Span>? _children;

        /// <summary>
        /// Child spans.
        /// </summary>
        public IReadOnlyList<Span> Children => _children ??= new List<Span>();

        /// <summary>
        /// Initializes an instance of <see cref="Transaction"/>.
        /// </summary>
        internal Transaction(
            IHub hub,
            string name,
            SentryId? spanId = null,
            SentryId? parentSpanId = null,
            string operation = "unknown")
        {
            _hub = hub;
            Name = name;
            SpanId = spanId ?? SentryId.Create();
            ParentSpanId = parentSpanId;
            TraceId = SentryId.Create();
            StartTimestamp = DateTimeOffset.Now;
            Operation = operation;
        }

        /// <summary>
        /// Initializes an instance of <see cref="Transaction"/>.
        /// </summary>
        public Transaction(IHub hub, string name, string operation)
            : this(
                hub,
                name,
                null,
                null,
                operation)
        {
        }

        /// <inheritdoc />
        public ISpan StartChild(string operation)
        {
            var span = new Span(null, SpanId, operation);
            (_children ??= new List<Span>()).Add(span);

            return span;
        }

        /// <inheritdoc />
        public void Finish(SpanStatus status = SpanStatus.Ok)
        {
            EndTimestamp = DateTimeOffset.Now;
            Status = status;

            _hub.CaptureTransaction(this);
        }

        /// <summary>
        /// Get Sentry trace header.
        /// </summary>
        public SentryTraceHeader GetTraceHeader() => new SentryTraceHeader(
            TraceId,
            SpanId,
            IsSampled
        );

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            // Transaction has a weird structure where some of the fields
            // are apparently stored inside "contexts.trace" object for
            // unknown reasons.

            writer.WriteStartObject();

            writer.WriteString("type", "transaction");
            writer.WriteString("event_id", SentryId.Create().ToString());

            if (!string.IsNullOrWhiteSpace(Name))
            {
                writer.WriteString("transaction", Name);
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

            if (_children is {} children && children.Any())
            {
                writer.WriteStartArray("spans");

                foreach (var i in children)
                {
                    writer.WriteSerializableValue(i);
                }

                writer.WriteEndArray();
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
        /// Parses transaction from JSON.
        /// </summary>
        public static Transaction FromJson(JsonElement json)
        {
            var hub = HubAdapter.Instance;

            var name = json.GetProperty("name").GetStringOrThrow();
            var spanId = json.GetPropertyOrNull("span_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
            var parentSpanId = json.GetPropertyOrNull("parent_span_id")?.Pipe(SentryId.FromJson);
            var traceId = json.GetPropertyOrNull("trace_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
            var startTimestamp = json.GetProperty("start_timestamp").GetDateTimeOffset();
            var endTimestamp = json.GetProperty("timestamp").GetDateTimeOffset();
            var operation = json.GetPropertyOrNull("op")?.GetString() ?? "unknown";
            var description = json.GetPropertyOrNull("description")?.GetString();
            var status = json.GetPropertyOrNull("status")?.GetString()?.Pipe(s => s.ParseEnum<SpanStatus>());
            var sampled = json.GetPropertyOrNull("sampled")?.GetBoolean() ?? false;
            var tags = json.GetPropertyOrNull("tags")?.GetDictionary()?.ToDictionary();
            var data = json.GetPropertyOrNull("data")?.GetObjectDictionary()?.ToDictionary();
            var children = json.GetPropertyOrNull("spans")?.EnumerateArray().Select(Span.FromJson).ToList();

            return new Transaction(hub, name, spanId, parentSpanId, operation)
            {
                TraceId = traceId,
                StartTimestamp = startTimestamp,
                EndTimestamp = endTimestamp,
                Description = description,
                Status = status,
                IsSampled = sampled,
                _tags = tags!,
                _data = data!,
                _children = children
            };
        }
    }
}

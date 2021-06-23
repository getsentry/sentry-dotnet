﻿using System;
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
        /// Initializes an instance of <see cref="SpanTracer"/>.
        /// </summary>
        public Span(SpanId? parentSpanId, string operation)
        {
            SpanId = SpanId.Create();
            ParentSpanId = parentSpanId;
            TraceId = SentryId.Create();
            Operation = operation;
        }

        /// <summary>
        /// Initializes an instance of <see cref="SpanTracer"/>.
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
        }

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
            writer.WriteSerializableIfNotNull("parent_span_id", ParentSpanId);
            writer.WriteSerializable("trace_id", TraceId);
            writer.WriteStringIfNotWhiteSpace("op", Operation);
            writer.WriteStringIfNotWhiteSpace("description", Description);
            writer.WriteStringIfNotWhiteSpace("status", Status?.ToString().ToSnakeCase());
            writer.WriteString("start_timestamp", StartTimestamp);
            writer.WriteStringIfNotNull("timestamp", EndTimestamp);
            writer.WriteStringDictionaryIfNotEmpty("tags", _tags!);
            writer.WriteDictionaryIfNotEmpty("data", _extra!);

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
                _extra = data!
            };
        }
    }
}

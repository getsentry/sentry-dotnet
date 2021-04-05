using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Sentry
{
    /// <summary>
    /// Transaction span tracer.
    /// </summary>
    public class SpanTracer : ISpan
    {
        private readonly IHub _hub;
        private readonly TransactionTracer _transaction;

        /// <inheritdoc />
        public SpanId SpanId { get; }

        /// <inheritdoc />
        public SpanId? ParentSpanId { get; }

        /// <inheritdoc />
        public SentryId TraceId { get; }

        /// <inheritdoc />
        public DateTimeOffset StartTimestamp { get; } = DateTimeOffset.UtcNow;

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
        /// Initializes an instance of <see cref="SpanTracer"/>.
        /// </summary>
        public SpanTracer(
            IHub hub,
            TransactionTracer transaction,
            SpanId? parentSpanId,
            SentryId traceId,
            string operation)
        {
            _hub = hub;
            _transaction = transaction;

            SpanId = SpanId.Create();
            ParentSpanId = parentSpanId;
            TraceId = traceId;
            Operation = operation;
        }

        /// <inheritdoc />
        public ISpan StartChild(string operation) =>
            _transaction.StartChild(SpanId, operation);

        /// <inheritdoc />
        public void Finish(SpanStatus status = SpanStatus.Ok)
        {
            EndTimestamp = DateTimeOffset.UtcNow;
            Status = status;
        }

        /// <inheritdoc />
        public void Finish(Exception exception, SpanStatus status)
        {
            _hub.BindException(exception, this);
            Finish(status);
        }

        /// <inheritdoc />
        public void Finish(Exception exception) =>
            Finish(exception, SpanStatusConverter.FromException(exception));

        /// <inheritdoc />
        public SentryTraceHeader GetTraceHeader() => new(
            TraceId,
            SpanId,
            IsSampled
        );
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Sentry
{
    /// <summary>
    /// Transaction span tracer.
    /// </summary>
    public class SpanTracer : ISpanTracer
    {
        private readonly IHub _hub;
        private readonly TransactionTracer _transaction;

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

        /// <inheritdoc cref="ISpanTracer.Operation" />
        public string Operation { get; set; }

        /// <inheritdoc cref="ISpanTracer.Description" />
        public string? Description { get; set; }

        /// <inheritdoc cref="ISpanTracer.Status" />
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
        public SpanTracer(IHub hub, TransactionTracer transaction, SpanId? parentSpanId, string operation)
        {
            _hub = hub;
            _transaction = transaction;

            SpanId = SpanId.Create();
            ParentSpanId = parentSpanId;
            TraceId = SentryId.Create();
            Operation = operation;
        }

        /// <inheritdoc />
        public ISpanTracer StartChild(string operation) =>
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

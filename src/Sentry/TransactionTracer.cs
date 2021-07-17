﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sentry
{
    /// <summary>
    /// Transaction tracer.
    /// </summary>
    public class TransactionTracer : ITransaction
    {
        private readonly IHub _hub;

        /// <inheritdoc />
        public SpanId SpanId
        {
            get => Contexts.Trace.SpanId;
            private set => Contexts.Trace.SpanId = value;
        }

        // A transaction normally does not have a parent because it represents
        // the top node in the span hierarchy.
        // However, a transaction may also be continued from a trace header
        // (i.e. when another service sends a request to this service),
        // in which case the newly created transaction refers to the incoming
        // transaction as the parent.

        /// <inheritdoc />
        public SpanId? ParentSpanId { get; }

        /// <inheritdoc />
        public SentryId TraceId
        {
            get => Contexts.Trace.TraceId;
            private set => Contexts.Trace.TraceId = value;
        }

        /// <inheritdoc cref="ITransaction.Name" />
        public string Name { get; set; }

        /// <inheritdoc cref="ITransaction.IsParentSampled" />
        public bool? IsParentSampled { get; set; }

        /// <inheritdoc />
        public string? Platform { get; set; } = Constants.Platform;

        /// <inheritdoc />
        public string? Release { get; set; }

        /// <inheritdoc />
        public DateTimeOffset StartTimestamp { get; } = DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public DateTimeOffset? EndTimestamp { get; internal set; }

        /// <inheritdoc cref="ISpan.Operation" />
        public string Operation
        {
            get => Contexts.Trace.Operation;
            set => Contexts.Trace.Operation = value;
        }

        /// <inheritdoc cref="ISpan.Description" />
        public string? Description { get; set; }

        /// <inheritdoc cref="ISpan.Status" />
        public SpanStatus? Status
        {
            get => Contexts.Trace.Status;
            set => Contexts.Trace.Status = value;
        }

        /// <inheritdoc />
        public bool? IsSampled
        {
            get => Contexts.Trace.IsSampled;
            internal set => Contexts.Trace.IsSampled = value;
        }

        /// <inheritdoc />
        public SentryLevel? Level { get; set; }

        private Request? _request;

        /// <inheritdoc />
        public Request Request
        {
            get => _request ??= new Request();
            set => _request = value;
        }

        private Contexts? _contexts;

        /// <inheritdoc />
        public Contexts Contexts
        {
            get => _contexts ??= new Contexts();
            set => _contexts = value;
        }

        private User? _user;

        /// <inheritdoc />
        public User User
        {
            get => _user ??= new User();
            set => _user = value;
        }

        /// <inheritdoc />
        public string? Environment { get; set; }

        // This field exists on SentryEvent and Scope, but not on Transaction
        string? IEventLike.TransactionName
        {
            get => Name;
            set => Name = value ?? "";
        }

        /// <inheritdoc />
        public SdkVersion Sdk { get; internal set; } = new();

        private IReadOnlyList<string>? _fingerprint;

        /// <inheritdoc />
        public IReadOnlyList<string> Fingerprint
        {
            get => _fingerprint ?? Array.Empty<string>();
            set => _fingerprint = value;
        }

        private readonly ConcurrentBag<Breadcrumb> _breadcrumbs = new();

        /// <inheritdoc />
        public IReadOnlyCollection<Breadcrumb> Breadcrumbs => _breadcrumbs;

        private readonly ConcurrentDictionary<string, object?> _extra = new();

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object?> Extra => _extra;

        private readonly ConcurrentDictionary<string, string> _tags = new();

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Tags => _tags;

        private readonly ConcurrentBag<SpanTracer> _spans = new();

        /// <inheritdoc />
        public IReadOnlyCollection<ISpan> Spans => _spans;

        /// <inheritdoc />
        public bool IsFinished => EndTimestamp is not null;

        /// <summary>
        /// Initializes an instance of <see cref="Transaction"/>.
        /// </summary>
        public TransactionTracer(IHub hub, string name, string operation)
        {
            _hub = hub;
            Name = name;
            SpanId = SpanId.Create();
            TraceId = SentryId.Create();
            Operation = operation;
        }

        /// <summary>
        /// Initializes an instance of <see cref="Transaction"/>.
        /// </summary>
        public TransactionTracer(IHub hub, ITransactionContext context)
            : this(hub, context.Name, context.Operation)
        {
            SpanId = context.SpanId;
            ParentSpanId = context.ParentSpanId;
            TraceId = context.TraceId;
            Description = context.Description;
            Status = context.Status;
            IsSampled = context.IsSampled;
        }

        /// <inheritdoc />
        public void AddBreadcrumb(Breadcrumb breadcrumb) =>
            _breadcrumbs.Add(breadcrumb);

        /// <inheritdoc />
        public void SetExtra(string key, object? value) =>
            _extra[key] = value;

        /// <inheritdoc />
        public void SetTag(string key, string value) =>
            _tags[key] = value;

        /// <inheritdoc />
        public void UnsetTag(string key) =>
            _tags.TryRemove(key, out _);

        internal ISpan StartChild(SpanId parentSpanId, string operation)
        {
            // Limit spans to 1000
            var isOutOfLimit = _spans.Count >= 1000;

            var span = new SpanTracer(_hub, this, parentSpanId, TraceId, operation)
            {
                IsSampled = !isOutOfLimit
                    ? IsSampled
                    : false // sample out out-of-limit spans
            };

            if (!isOutOfLimit)
            {
                _spans.Add(span);
            }

            return span;
        }

        /// <inheritdoc />
        public ISpan StartChild(string operation) =>
            StartChild(SpanId, operation);

        /// <inheritdoc />
        public void Finish()
        {
            try
            {
                Status ??= SpanStatus.UnknownError;
                EndTimestamp = DateTimeOffset.UtcNow;

                // Client decides whether to discard this transaction based on sampling
                _hub.CaptureTransaction(new Transaction(this));
            }
            finally
            {
                // Clear the transaction from the scope
                _hub.ConfigureScope(scope => scope.ResetTransaction(this));
            }
        }

        /// <inheritdoc />
        public void Finish(SpanStatus status)
        {
            Status = status;
            Finish();
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
        public ISpan? GetLastActiveSpan() => 
            // We need to sort by timestamp because the order of ConcurrentBag<T> is not deterministic
            Spans.OrderByDescending(x => x.StartTimestamp).FirstOrDefault(s => !s.IsFinished);

        /// <inheritdoc />
        public SentryTraceHeader GetTraceHeader() => new(
            TraceId,
            SpanId,
            IsSampled
        );
    }
}

using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry;

/// <summary>
/// Transaction tracer.
/// </summary>
public class TransactionTracer : ITransaction, IHasDistribution, IHasTransactionNameSource, IHasMeasurements
{
    private readonly IHub _hub;
    private readonly SentryOptions? _options;
    private readonly Timer? _idleTimer;
    private long _cancelIdleTimeout;
    private readonly SentryStopwatch _stopwatch = SentryStopwatch.StartNew();
    private readonly Instrumenter _instrumenter = Instrumenter.Sentry;

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

    /// <inheritdoc cref="IHasTransactionNameSource.NameSource" />
    public TransactionNameSource NameSource { get; set; }

    /// <inheritdoc cref="ITransaction.IsParentSampled" />
    public bool? IsParentSampled { get; set; }

    /// <inheritdoc />
    public string? Platform { get; set; } = Constants.Platform;

    /// <inheritdoc />
    public string? Release { get; set; }

    /// <inheritdoc />
    public string? Distribution { get; set; }

    /// <inheritdoc />
    public DateTimeOffset StartTimestamp { get; internal set; }

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
        internal set
        {
            Contexts.Trace.IsSampled = value;
            SampleRate ??= value == null ? null : value.Value ? 1.0 : 0.0;
        }
    }

    /// <summary>
    /// The sample rate used for this transaction.
    /// </summary>
    public double? SampleRate { get; internal set; }

    /// <inheritdoc />
    public SentryLevel? Level { get; set; }

    private Request? _request;

    /// <inheritdoc />
    public Request Request
    {
        get => _request ??= new Request();
        set => _request = value;
    }

    private readonly Contexts _contexts = new();

    /// <inheritdoc />
    public Contexts Contexts
    {
        get => _contexts;
        set => _contexts.ReplaceWith(value);
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

    private readonly ConcurrentDictionary<string, Measurement> _measurements = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, Measurement> Measurements => _measurements;

    /// <inheritdoc />
    public bool IsFinished => EndTimestamp is not null;

    internal DynamicSamplingContext? DynamicSamplingContext { get; set; }

    internal ITransactionProfiler? TransactionProfiler { get; set; }

    /// <summary>
    /// Used by the Sentry.OpenTelemetry.SentrySpanProcessor to mark a transaction as a Sentry request. Ideally we wouldn't
    /// create this transaction but since we can't avoid doing that, once we detect that it's a Sentry request we mark it
    /// as such so that we can prevent finishing the transaction tracer when idle timeout elapses and the TransactionTracer gets converted into
    /// a Transaction.
    /// </summary>
    internal bool IsSentryRequest { get; set; }

    // TODO: mark as internal in version 4
    /// <summary>
    /// Initializes an instance of <see cref="Transaction"/>.
    /// </summary>
    public TransactionTracer(IHub hub, string name, string operation)
        : this(hub, name, operation, TransactionNameSource.Custom)
    {
    }

    // TODO: mark as internal in version 4
    /// <summary>
    /// Initializes an instance of <see cref="Transaction"/>.
    /// </summary>
    public TransactionTracer(IHub hub, string name, string operation, TransactionNameSource nameSource)
    {
        _hub = hub;
        _options = _hub.GetSentryOptions();
        Name = name;
        NameSource = nameSource;
        SpanId = SpanId.Create();
        TraceId = SentryId.Create();
        Operation = operation;
        StartTimestamp = _stopwatch.StartDateTimeOffset;
    }

    /// <summary>
    /// Initializes an instance of <see cref="TransactionTracer"/>.
    /// </summary>
    public TransactionTracer(IHub hub, ITransactionContext context) : this(hub, context, null)
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="TransactionTracer"/>.
    /// </summary>
    internal TransactionTracer(IHub hub, ITransactionContext context, TimeSpan? idleTimeout = null)
    {
        _hub = hub;
        _options = _hub.GetSentryOptions();
        Name = context.Name;
        NameSource = context is IHasTransactionNameSource c ? c.NameSource : TransactionNameSource.Custom;
        Operation = context.Operation;
        SpanId = context.SpanId;
        ParentSpanId = context.ParentSpanId;
        TraceId = context.TraceId;
        Description = context.Description;
        Status = context.Status;
        IsSampled = context.IsSampled;
        StartTimestamp = _stopwatch.StartDateTimeOffset;

		if (context is TransactionContext transactionContext)
        {
            _instrumenter = transactionContext.Instrumenter;
        }

        // Set idle timer only if an idle timeout has been provided directly
        if (idleTimeout.HasValue)
        {
            _cancelIdleTimeout = 1;  // Timer will be cancelled once, atomically setting this back to 0
            _idleTimer = new Timer(state =>
            {
                if (state is not TransactionTracer transactionTracer)
                {
                    _options?.LogDebug(
                        $"Idle timeout callback received nor non-TransactionTracer state. " +
                        "Unable to finish transaction automatically."
                    );
                    return;
                }

                transactionTracer.Finish(Status ?? SpanStatus.Ok);
            }, this, idleTimeout.Value, Timeout.InfiniteTimeSpan);
        }
    }

    /// <inheritdoc />
    public void AddBreadcrumb(Breadcrumb breadcrumb) => _breadcrumbs.Add(breadcrumb);

    /// <inheritdoc />
    public void SetExtra(string key, object? value) => _extra[key] = value;

    /// <inheritdoc />
    public void SetTag(string key, string value) => _tags[key] = value;

    /// <inheritdoc />
    public void UnsetTag(string key) => _tags.TryRemove(key, out _);

    /// <inheritdoc />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void SetMeasurement(string name, Measurement measurement) => _measurements[name] = measurement;

    /// <inheritdoc />
    public ISpan StartChild(string operation) => StartChild(spanId: null, parentSpanId: SpanId, operation);

    internal ISpan StartChild(SpanId? spanId, SpanId parentSpanId, string operation,
        Instrumenter instrumenter = Instrumenter.Sentry)
    {
        if (instrumenter != _instrumenter)
        {
            _options?.LogWarning(
                "Attempted to create a span via {0} instrumentation to a span or transaction" +
                " originating from {1} instrumentation. The span will not be created.", instrumenter, _instrumenter);
            return NoOpSpan.Instance;
        }

        var span = new SpanTracer(_hub, this, parentSpanId, TraceId, operation);
        if (spanId is { } id)
        {
            span.SpanId = id;
        }

        AddChildSpan(span);
        return span;
    }

    private void AddChildSpan(SpanTracer span)
    {
        // Limit spans to 1000
        var isOutOfLimit = _spans.Count >= 1000;
        span.IsSampled = isOutOfLimit ? false : IsSampled;

        if (!isOutOfLimit)
        {
            _spans.Add(span);
            ActiveSpanTracker.Push(span);
        }
    }

    class LastActiveSpanTracker
    {
        private Mutex mut = new Mutex();

        private Stack<ISpan> trackedSpans = new();
        public void Push(ISpan span)
        {
            mut.WaitOne();
            try
            {
                trackedSpans.Push(span);
            }
            finally
            {
                mut.ReleaseMutex();
            }
        }

        public ISpan? Peek()
        {
            mut.WaitOne();
            try
            {
                while (trackedSpans.Count > 0)
                {
                    // Stop tracking inactive spans
                    var span = trackedSpans.Peek();
                    if (!span.IsFinished)
                    {
                        return span;
                    }
                    trackedSpans.Pop();
                }
                return null;
            }
            finally
            {
                mut.ReleaseMutex();
            }
        }
    }
    private LastActiveSpanTracker ActiveSpanTracker => new LastActiveSpanTracker();

    /// <inheritdoc />
    public ISpan? GetLastActiveSpan() => ActiveSpanTracker.Peek();

    /// <inheritdoc />
    public void Finish()
    {
        _options?.LogDebug("Attempting to finish Transaction {0}.", SpanId);
        if (Interlocked.Exchange(ref _cancelIdleTimeout, 0) == 1)
        {
            _options?.LogDebug("Disposing of idle timer for Transaction {0}.", SpanId);
            _idleTimer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _idleTimer?.Dispose();
        }

        if (IsSentryRequest)
        {
            // Normally we wouldn't start transactions for Sentry requests but when instrumenting with OpenTelemetry
            // we are only able to determine whether it's a sentry request or not when closing a span... we leave these
            // to be garbage collected and we don't want idle timers triggering on them
            _options?.LogDebug("Transaction {0} is a Sentry Request. Don't complete.", SpanId);
            return;
        }

        TransactionProfiler?.Finish();
        Status ??= SpanStatus.Ok;
        EndTimestamp ??= _stopwatch.CurrentDateTimeOffset;
        _options?.LogDebug("Finished Transaction {0}.", SpanId);

        foreach (var span in _spans)
        {
            if (!span.IsFinished)
            {
                _options?.LogDebug("Deadline exceeded for Transaction {0} -> Span {1}.", SpanId, span.SpanId);
                span.Finish(SpanStatus.DeadlineExceeded);
            }
        }

        // Clear the transaction from the scope
        _hub.ConfigureScope(scope => scope.ResetTransaction(this));

        // Client decides whether to discard this transaction based on sampling
        _hub.CaptureTransaction(new Transaction(this));
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
    public SentryTraceHeader GetTraceHeader() => new(TraceId, SpanId, IsSampled);
}

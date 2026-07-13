using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry;

/// <summary>
/// Transaction tracer.
/// </summary>
public sealed class TransactionTracer : IBaseTracer, IAutoTimeoutTracer, ITransactionTracer
{
    private const int SpanLimit = 1000;

    private readonly IHub _hub;
    private readonly SentryOptions? _options;
    private readonly ISentryTimer? _idleTimer;

    /// <summary>
    /// Spike: invoked once the transaction transitions to finished (captured or discarded), including
    /// out-of-band finishes from the idle timer. Used by the Activity tracing shim to stop the backing
    /// Activity when the tracer finishes without the Activity lifecycle being involved.
    /// </summary>
    internal Action? OnFinished { get; set; }
    private readonly TimeSpan? _idleTimeout;
    private readonly SentryStopwatch _stopwatch = SentryStopwatch.StartNew();

    /// <summary>
    /// Set exactly once inside the `_lock` at the same time as setting `_endTimestamp` and disposing the idle timer.
    /// `IsFinished` makes a volatile read of `_hasFinished` (no lock required as would be necessary for the more
    /// complex `_endTimestamp` struct), which is essentially why we need this separate flag.
    /// </summary>
    private bool _hasFinished;
    private readonly Lock _lock = new();

    private readonly Instrumenter _instrumenter = Instrumenter.Sentry;

    bool IBaseTracer.IsOtelInstrumenter => _instrumenter == Instrumenter.OpenTelemetry;

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

    /// <inheritdoc cref="ITransactionTracer.Name" />
    public string Name { get; set; }

    /// <inheritdoc cref="ITransactionContext.NameSource" />
    public TransactionNameSource NameSource { get; set; }

    /// <inheritdoc cref="ITransactionTracer.IsParentSampled" />
    public bool? IsParentSampled { get; set; }

    /// <inheritdoc />
    public string? Platform { get; set; } = SentryConstants.Platform;

    /// <inheritdoc />
    public string? Release { get; set; }

    /// <inheritdoc />
    public string? Distribution { get; set; }

    /// <inheritdoc />
    public DateTimeOffset StartTimestamp { get; internal set; }

    /// <summary>
    /// Guard writes with `_lock`
    /// </summary>
    private DateTimeOffset? _endTimestamp;

    /// <inheritdoc />
    public DateTimeOffset? EndTimestamp
    {
        // get => _endTimestamp;
        get => Volatile.Read(ref _hasFinished) ? _endTimestamp : null;
        internal set
        {
            lock (_lock)
            {
                _endTimestamp = value;
            }
        }
    }

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
    public bool? IsSampled => true; // Implicitly if we instantiate this class then the transaction is sampled in

    /// <summary>
    /// The sample rate used for this transaction.
    /// </summary>
    public double? SampleRate { get; internal set; }

    internal double? SampleRand { get; set; }

    /// <inheritdoc />
    public SentryLevel? Level { get; set; }

    private SentryRequest? _request;

    /// <inheritdoc />
    public SentryRequest Request
    {
        get => _request ??= new SentryRequest();
        set => _request = value;
    }

    private readonly SentryContexts _contexts = new();

    /// <inheritdoc />
    public SentryContexts Contexts
    {
        get => _contexts;
        set => _contexts.ReplaceWith(value);
    }

    private SentryUser? _user;

    /// <inheritdoc />
    public SentryUser User
    {
        get => _user ??= new SentryUser();
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

    private readonly ConcurrentBagLite<Breadcrumb> _breadcrumbs = new();

    /// <inheritdoc />
    public IReadOnlyCollection<Breadcrumb> Breadcrumbs => _breadcrumbs;

    /// <inheritdoc />
    [Obsolete("Use Data")]
    public IReadOnlyDictionary<string, object?> Extra => _contexts.Trace.Data;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Data => _contexts.Trace.Data;

    private readonly ConcurrentDictionary<string, string> _tags = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Tags => _tags;

    private readonly ConcurrentBagLite<ISpan> _spans = new();

    /// <inheritdoc />
    public IReadOnlyCollection<ISpan> Spans => _spans;

    private readonly ConcurrentDictionary<string, Measurement> _measurements = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, Measurement> Measurements => _measurements;

    private readonly Lazy<MetricsSummaryAggregator> _metricsSummary = new();
    internal MetricsSummaryAggregator MetricsSummary => _metricsSummary.Value;
    internal bool HasMetrics => _metricsSummary.IsValueCreated;

    /// <inheritdoc />
    public bool IsFinished => Volatile.Read(ref _hasFinished);

    internal DynamicSamplingContext? DynamicSamplingContext { get; set; }

    internal ITransactionProfiler? TransactionProfiler { get; set; }

    /// <summary>
    /// Used by the Sentry.OpenTelemetry.SentrySpanProcessor to mark a transaction as a Sentry request. Ideally we wouldn't
    /// create this transaction but since we can't avoid doing that, once we detect that it's a Sentry request we mark it
    /// as such so that we can prevent finishing the transaction tracer when idle timeout elapses and the TransactionTracer gets converted into
    /// a Transaction.
    /// </summary>
    internal bool IsSentryRequest { get; set; }

    /// <summary>
    /// Initializes an instance of <see cref="TransactionTracer"/>.
    /// </summary>
    public TransactionTracer(IHub hub, ITransactionContext context) : this(hub, context, null)
    {
    }

    /// <summary>
    /// Initializes an instance of <see cref="SentryTransaction"/>.
    /// </summary>
    internal TransactionTracer(IHub hub, string name, string operation, TransactionNameSource nameSource = TransactionNameSource.Custom)
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
    internal TransactionTracer(IHub hub, ITransactionContext context, TimeSpan? idleTimeout = null,
        Func<Action, ISentryTimer>? timerFactory = null)
    {
        _hub = hub;
        _options = _hub.GetSentryOptions();
        Name = context.Name;
        NameSource = context.NameSource;
        Operation = context.Operation;
        SpanId = context.SpanId;
        ParentSpanId = context.ParentSpanId;
        TraceId = context.TraceId;
        Description = context.Description;
        Status = context.Status;
        StartTimestamp = _stopwatch.StartDateTimeOffset;

        if (context is TransactionContext transactionContext)
        {
            _instrumenter = transactionContext.Instrumenter;
            Origin = transactionContext.Origin;
        }

        if (idleTimeout.HasValue)
        {
            _idleTimeout = idleTimeout;
            var factory = timerFactory ?? (cb => new SystemTimer(cb));
            _idleTimer = factory(OnIdleTimeout);
            _idleTimer.Start(idleTimeout.Value);
        }
    }

    private void OnIdleTimeout()
    {
        if (IsSentryRequest)
        {
            _options?.LogDebug("Transaction '{0}' is a Sentry Request. Don't complete.", SpanId);
            return;
        }

        if (!TryBeginFinish(fromIdleTimer: true, out var shouldDiscard))
        {
            return;
        }

        if (shouldDiscard)
        {
            _options?.LogDebug("Idle transaction '{0}' has no child spans. Discarding.", SpanId);
            _hub.ConfigureScope(static (scope, tracer) => scope.ResetTransaction(tracer), this);
            OnFinished?.Invoke();
            return;
        }

        Status ??= SpanStatus.Ok;
        CompleteCapture();
        OnFinished?.Invoke();
    }

    /// <inheritdoc />
    public void AddBreadcrumb(Breadcrumb breadcrumb) => _breadcrumbs.Add(breadcrumb);

    /// <inheritdoc />
    [Obsolete("Use SetData")]
    public void SetExtra(string key, object? value) => _contexts.Trace.SetData(key, value);

    /// <inheritdoc />
    public void SetData(string key, object? value) => _contexts.Trace.SetData(key, value);

    /// <inheritdoc />
    public void SetTag(string key, string value) => _tags[key] = value;

    /// <inheritdoc />
    public void UnsetTag(string key) => _tags.TryRemove(key, out _);

    /// <inheritdoc />
    public void SetMeasurement(string name, Measurement measurement) => _measurements[name] = measurement;

    /// <inheritdoc />
    public ISpan StartChild(string operation) => StartChild(spanId: null, parentSpanId: SpanId, operation);

    internal ISpan StartChild(SpanId? spanId, SpanId parentSpanId, string operation,
        Instrumenter instrumenter = Instrumenter.Sentry)
    {
        var span = new SpanTracer(_hub, this, SpanId.Create(), parentSpanId, TraceId, operation, instrumenter: instrumenter);
        if (spanId is { } id)
        {
            span.SpanId = id;
        }

        AddChildSpan(span);
        return span;
    }

    private void AddChildSpan(SpanTracer span)
    {
        lock (_lock)
        {
            if (_hasFinished)
            {
                _options?.LogDebug("Discarding child span '{0}' as the trace has already finished", SpanId);
                span.IsSampled = false;
                return;
            }

            if (_spans.Count >= SpanLimit)
            {
                _options?.LogDebug("Discarding child span '{0}' due to {1} span limit", SpanId, SpanLimit);
                span.IsSampled = false;
                return;
            }

            span.IsSampled = IsSampled;
            _idleTimer?.Cancel();
            _spans.Add(span);
            _activeSpanTracker.Push(span);
        }
    }

    internal void ChildSpanFinished()
    {
        // Fast path: only idle-timeout transactions need to react to child finishes.
        // `_idleTimeout` is readonly, so this check is safe outside the lock
        if (!_idleTimeout.HasValue)
        {
            return;
        }
        lock (_lock)
        {
            if (_hasFinished)
            {
                return;
            }

            // Only restart the idle timer when there are no more active (unfinished) child spans
            if (_activeSpanTracker.PeekActive() == null)
            {
                _idleTimer?.Start(_idleTimeout.Value);
            }
        }
    }

    private class LastActiveSpanTracker
    {
        private readonly Lazy<Stack<ISpan>> _trackedSpans = new();
        private Stack<ISpan> TrackedSpans => _trackedSpans.Value;

        public void Push(ISpan span) => TrackedSpans.Push(span);

        public ISpan? PeekActive()
        {
            // Non-destructive: leave finished spans in place so SpanTracer.Unfinish() can resurrect them.
            foreach (var span in TrackedSpans)
            {
                if (!span.IsFinished)
                {
                    return span;
                }
            }
            return null;
        }

        public void Clear() => TrackedSpans.Clear();
    }
    private readonly LastActiveSpanTracker _activeSpanTracker = new LastActiveSpanTracker();

    /// <inheritdoc />
    public ISpan? GetLastActiveSpan()
    {
        lock (_lock)
        {
            return _activeSpanTracker.PeekActive();
        }
    }

    void IAutoTimeoutTracer.ResetIdleTimeout()
    {
        lock (_lock)
        {
            if (!_idleTimeout.HasValue || _hasFinished)
            {
                return;
            }
            _idleTimer?.Start(_idleTimeout.Value);
        }
    }

    /// <summary>
    /// The single atomic primitive for transitioning Running -> Finished. Returns true to
    /// exactly one caller; all subsequent calls return false. The Running -> Finished flip,
    /// the `EndTimestamp` write, and the timer disposal all happen inside the same critical
    /// section, so no observer can ever see an inconsistent intermediate state.
    /// </summary>
    /// <param name="fromIdleTimer">
    /// True when called from the idle-timer callback. In that case we also re-check whether
    /// a child span is still active (it might have been added after the timer fired but
    /// before we acquired the lock); if so, the firing is stale, and we refuse to finish.
    /// </param>
    /// <param name="shouldDiscard">
    /// True if the transaction had no child spans and the caller (idle timer) should
    /// discard rather than capture.
    /// </param>
    private bool TryBeginFinish(bool fromIdleTimer, out bool shouldDiscard)
    {
        shouldDiscard = false;
        lock (_lock)
        {
            if (_hasFinished)
            {
                return false;
            }

            if (fromIdleTimer)
            {
                // Defensive re-check: if a child span arrived between the timer firing and
                // us acquiring the lock, AddChildSpan's `Cancel()` may have failed to stop
                // the in-flight callback. The active-span check inside the lock is the
                // authoritative answer.
                if (_activeSpanTracker.PeekActive() != null)
                {
                    return false;
                }

                if (_spans.IsEmpty)
                {
                    shouldDiscard = true;
                }
            }

            // Compute the end timestamp inside the lock. For idle transactions, trim to
            // the latest finished child span when available. (Scanning `_spans` here is
            // bounded by SpanLimit and only runs once per transaction.)
            DateTimeOffset endTimestamp;
            if (_idleTimeout.HasValue && !shouldDiscard)
            {
                DateTimeOffset latest = default;
                foreach (var s in _spans)
                {
                    if (s.IsFinished && s.EndTimestamp is { } et && et > latest)
                    {
                        latest = et;
                    }
                }
                endTimestamp = latest == default ? _stopwatch.CurrentDateTimeOffset : latest;
            }
            else
            {
                endTimestamp = _endTimestamp ?? _stopwatch.CurrentDateTimeOffset;
            }

            _endTimestamp = endTimestamp;
            _idleTimer?.Cancel();
            _idleTimer?.Dispose();

            // MUST be the last write inside the lock for all logic that depends on volatile reads to work
            Volatile.Write(ref _hasFinished, true);
            return true;
        }
    }

    /// <inheritdoc />
    public void Finish()
    {
        if (!TryBeginFinish(fromIdleTimer: false, out _))
        {
            return;
        }

        _options?.LogDebug("Attempting to finish Transaction '{0}'.", SpanId);
        if (IsSentryRequest)
        {
            // Normally we wouldn't start transactions for Sentry requests but when instrumenting with OpenTelemetry
            // we are only able to determine whether it's a sentry request or not when closing a span... we leave these
            // to be garbage collected. The idle timer has already been disposed inside TryBeginFinish.
            _options?.LogDebug("Transaction '{0}' is a Sentry Request. Don't complete.", SpanId);
            return;
        }

        Status ??= SpanStatus.Ok;
        CompleteCapture();
        OnFinished?.Invoke();
    }

    /// <summary>
    /// Performs non-locked work that follows a successful Running -> Finished transition
    /// </summary>
    private void CompleteCapture()
    {
        TransactionProfiler?.Finish();

        _options?.LogDebug("Finished Transaction '{0}'.", SpanId);

        // Clear the transaction from the scope and regenerate the Propagation Context
        // We do this so new events don't have a trace context that is "older" than the transaction that just finished
        _hub.ConfigureScope(static (scope, transactionTracer) => scope.ResetTransaction(transactionTracer), this);

        // Client decides whether to discard this transaction based on sampling
        _hub.CaptureTransaction(new SentryTransaction(this));

        // Release tracked spans
        ReleaseSpans();
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

    /// <inheritdoc />
    public string? Origin
    {
        get => Contexts.Trace.Origin;
        internal set => Contexts.Trace.Origin = value;
    }

    private void ReleaseSpans()
    {
        _spans.Clear();
        _activeSpanTracker.Clear();
    }

    /// <summary>
    /// <para>
    /// Automatically finishes the transaction with a status of <see cref="SpanStatus.Ok" /> at the end of a
    /// <c>using</c> block, if it has not already been finished.
    /// </para>
    /// <para>
    /// This is the equivalent of calling <see cref="Finish()" /> when the transaction passes out of scope.
    /// </para>
    /// </summary>
    /// <remarks>This is a convenience method only. Disposing is not required.</remarks>
    public void Dispose()
    {
        Finish();
    }
}

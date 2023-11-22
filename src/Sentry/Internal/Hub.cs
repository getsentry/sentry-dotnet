using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Integrations;
using Sentry.Internal.ScopeStack;

namespace Sentry.Internal;

internal class Hub : IHub, IDisposable
{
    private readonly object _sessionPauseLock = new();

    private readonly ISentryClient _ownedClient;
    private readonly ISystemClock _clock;
    private readonly ISessionManager _sessionManager;
    private readonly SentryOptions _options;
    private readonly RandomValuesFactory _randomValuesFactory;

    private int _isPersistedSessionRecovered;

    // Internal for testability
    internal ConditionalWeakTable<Exception, ISpan> ExceptionToSpanMap { get; } = new();

    internal IInternalScopeManager ScopeManager { get; }

    private int _isEnabled = 1;
    public bool IsEnabled => _isEnabled == 1;

    internal SentryOptions Options => _options;

    internal Hub(
        SentryOptions options,
        ISentryClient? client = null,
        ISessionManager? sessionManager = null,
        ISystemClock? clock = null,
        IInternalScopeManager? scopeManager = null,
        RandomValuesFactory? randomValuesFactory = null)
    {
        if (string.IsNullOrWhiteSpace(options.Dsn))
        {
            const string msg = "Attempt to instantiate a Hub without a DSN.";
            options.LogFatal(msg);
            throw new InvalidOperationException(msg);
        }

        options.LogDebug("Initializing Hub for Dsn: '{0}'.", options.Dsn);

        _options = options;
        _randomValuesFactory = randomValuesFactory ?? new SynchronizedRandomValuesFactory();
        _ownedClient = client ?? new SentryClient(options, randomValuesFactory: _randomValuesFactory);
        _clock = clock ?? SystemClock.Clock;
        _sessionManager = sessionManager ?? new GlobalSessionManager(options);

        ScopeManager = scopeManager ?? new SentryScopeManager(options, _ownedClient);

        if (!options.IsGlobalModeEnabled)
        {
            // Push the first scope so the async local starts from here
            PushScope();
        }

        foreach (var integration in options.Integrations)
        {
            options.LogDebug("Registering integration: '{0}'.", integration.GetType().Name);
            integration.Register(this, options);
        }
    }

    public void ConfigureScope(Action<Scope> configureScope)
    {
        try
        {
            ScopeManager.ConfigureScope(configureScope);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to ConfigureScope");
        }
    }

    public async Task ConfigureScopeAsync(Func<Scope, Task> configureScope)
    {
        try
        {
            await ScopeManager.ConfigureScopeAsync(configureScope).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to ConfigureScopeAsync");
        }
    }

    public IDisposable PushScope() => ScopeManager.PushScope();

    public IDisposable PushScope<TState>(TState state) => ScopeManager.PushScope(state);

    public void RestoreScope(Scope savedScope) => ScopeManager.RestoreScope(savedScope);

    public void BindClient(ISentryClient client) => ScopeManager.BindClient(client);

    public ITransactionTracer StartTransaction(
        ITransactionContext context,
        IReadOnlyDictionary<string, object?> customSamplingContext)
        => StartTransaction(context, customSamplingContext, null);

    internal ITransactionTracer StartTransaction(
        ITransactionContext context,
        IReadOnlyDictionary<string, object?> customSamplingContext,
        DynamicSamplingContext? dynamicSamplingContext)
    {
        var instrumenter = (context as SpanContext)?.Instrumenter;
        if (instrumenter != _options.Instrumenter)
        {
            _options.LogWarning(
                $"Attempted to start a transaction via {instrumenter} instrumentation when the SDK is" +
                $" configured for {_options.Instrumenter} instrumentation.  The transaction will not be created.");

            return NoOpTransaction.Instance;
        }

        var transaction = new TransactionTracer(this, context);

        // If the hub is disabled, we will always sample out.  In other words, starting a transaction
        // after disposing the hub will result in that transaction not being sent to Sentry.
        // Additionally, we will always sample out if tracing is explicitly disabled.
        // Do not invoke the TracesSampler, evaluate the TracesSampleRate, and override any sampling decision
        // that may have been already set (i.e.: from a sentry-trace header).
        if (!IsEnabled || _options.EnableTracing is false)
        {
            transaction.IsSampled = false;
            transaction.SampleRate = 0.0;
        }
        else
        {
            // Except when tracing is disabled, TracesSampler runs regardless of whether a decision
            // has already been made, as it can be used to override it.
            if (_options.TracesSampler is { } tracesSampler)
            {
                var samplingContext = new TransactionSamplingContext(
                    context,
                    customSamplingContext);

                if (tracesSampler(samplingContext) is { } sampleRate)
                {
                    transaction.IsSampled = _randomValuesFactory.NextBool(sampleRate);
                    transaction.SampleRate = sampleRate;
                }
            }

            // Random sampling runs only if the sampling decision hasn't been made already.
            if (transaction.IsSampled == null)
            {
                var sampleRate = _options.TracesSampleRate ?? (_options.EnableTracing is true ? 1.0 : 0.0);
                transaction.IsSampled = _randomValuesFactory.NextBool(sampleRate);
                transaction.SampleRate = sampleRate;
            }

            if (transaction.IsSampled is true && _options.TransactionProfilerFactory is { } profilerFactory)
            {
                // TODO cancellation token based on Hub being closed?
                transaction.TransactionProfiler = profilerFactory.Start(transaction, CancellationToken.None);
            }
        }

        // Use the provided DSC, or create one based on this transaction.
        // DSC creation must be done AFTER the sampling decision has been made.
        transaction.DynamicSamplingContext =
            dynamicSamplingContext ?? transaction.CreateDynamicSamplingContext(_options);

        // A sampled out transaction still appears fully functional to the user
        // but will be dropped by the client and won't reach Sentry's servers.
        return transaction;
    }

    public void BindException(Exception exception, ISpan span)
    {
        // Don't bind on sampled out spans
        if (span.IsSampled == false)
        {
            return;
        }

        // Don't overwrite existing pair in the unlikely event that it already exists
        _ = ExceptionToSpanMap.GetValue(exception, _ => span);
    }

    public ISpan? GetSpan() => ScopeManager.GetCurrent().Key.Span;

    public SentryTraceHeader GetTraceHeader()
    {
        if (GetSpan()?.GetTraceHeader() is { } traceHeader)
        {
            return traceHeader;
        }

        // With either tracing disabled or no active span on the current scope we fall back to the propagation context
        var propagationContext = ScopeManager.GetCurrent().Key.PropagationContext;
        // In either case, we must not append a sampling decision.
        return new SentryTraceHeader(propagationContext.TraceId, propagationContext.SpanId, null);
    }

    public BaggageHeader GetBaggage()
    {
        if (GetSpan() is TransactionTracer { DynamicSamplingContext: { IsEmpty: false } dsc } )
        {
            return dsc.ToBaggageHeader();
        }

        var propagationContext = ScopeManager.GetCurrent().Key.PropagationContext;
        return propagationContext.GetOrCreateDynamicSamplingContext(_options).ToBaggageHeader();
    }

    public TransactionContext ContinueTrace(
        string? traceHeader,
        string? baggageHeader,
        string? name = null,
        string? operation = null)
    {
        SentryTraceHeader? sentryTraceHeader = null;
        if (traceHeader is not null)
        {
            sentryTraceHeader = SentryTraceHeader.Parse(traceHeader);
        }

        BaggageHeader? sentryBaggageHeader = null;
        if (baggageHeader is not null)
        {
            sentryBaggageHeader = BaggageHeader.TryParse(baggageHeader, onlySentry: true);
        }

        return ContinueTrace(sentryTraceHeader, sentryBaggageHeader, name, operation);
    }

    public TransactionContext ContinueTrace(
        SentryTraceHeader? traceHeader,
        BaggageHeader? baggageHeader,
        string? name = null,
        string? operation = null)
    {
        var propagationContext = SentryPropagationContext.CreateFromHeaders(_options.DiagnosticLogger, traceHeader, baggageHeader);
        ConfigureScope(scope => scope.PropagationContext = propagationContext);

        // If we have to create a new SentryTraceHeader we don't make a sampling decision
        traceHeader ??= new SentryTraceHeader(propagationContext.TraceId, propagationContext.SpanId, null);
        return new TransactionContext(
            name ?? string.Empty,
            operation ?? string.Empty,
            traceHeader);
    }

    public void StartSession()
    {
        // Attempt to recover persisted session left over from previous run
        if (Interlocked.Exchange(ref _isPersistedSessionRecovered, 1) != 1)
        {
            try
            {
                var recoveredSessionUpdate = _sessionManager.TryRecoverPersistedSession();
                if (recoveredSessionUpdate is not null)
                {
                    CaptureSession(recoveredSessionUpdate);
                }
            }
            catch (Exception ex)
            {
                _options.LogError(ex, "Failed to recover persisted session.");
            }
        }

        // Start a new session
        try
        {
            var sessionUpdate = _sessionManager.StartSession();
            if (sessionUpdate is not null)
            {
                CaptureSession(sessionUpdate);
            }
        }
        catch (Exception ex)
        {
            _options.LogError(ex, "Failed to start a session.");
        }
    }

    public void PauseSession()
    {
        lock (_sessionPauseLock)
        {
            try
            {
                _sessionManager.PauseSession();
            }
            catch (Exception ex)
            {
                _options.LogError(ex, "Failed to pause a session.");
            }
        }
    }

    public void ResumeSession()
    {
        lock (_sessionPauseLock)
        {
            try
            {
                foreach (var update in _sessionManager.ResumeSession())
                {
                    CaptureSession(update);
                }
            }
            catch (Exception ex)
            {
                _options.LogError(ex, "Failed to resume a session.");
            }
        }
    }

    private void EndSession(DateTimeOffset timestamp, SessionEndStatus status)
    {
        try
        {
            var sessionUpdate = _sessionManager.EndSession(timestamp, status);
            if (sessionUpdate is not null)
            {
                CaptureSession(sessionUpdate);
            }
        }
        catch (Exception ex)
        {
            _options.LogError(ex, "Failed to end a session.");
        }
    }

    public void EndSession(SessionEndStatus status = SessionEndStatus.Exited) =>
        EndSession(_clock.GetUtcNow(), status);

    private ISpan? GetLinkedSpan(SentryEvent evt)
    {
        // Find the span which is bound to the same exception
        if (evt.Exception is { } exception &&
            ExceptionToSpanMap.TryGetValue(exception, out var spanBoundToException))
        {
            return spanBoundToException;
        }

        return null;
    }

    private void ApplyTraceContextToEvent(SentryEvent evt, ISpan span)
    {
        evt.Contexts.Trace.SpanId = span.SpanId;
        evt.Contexts.Trace.TraceId = span.TraceId;
        evt.Contexts.Trace.ParentSpanId = span.ParentSpanId;

        if (span.GetTransaction() is TransactionTracer transactionTracer)
        {
            evt.DynamicSamplingContext = transactionTracer.DynamicSamplingContext;
        }
    }

    private void ApplyTraceContextToEvent(SentryEvent evt, SentryPropagationContext propagationContext)
    {
        evt.Contexts.Trace.TraceId = propagationContext.TraceId;
        evt.Contexts.Trace.SpanId = propagationContext.SpanId;
        evt.Contexts.Trace.ParentSpanId = propagationContext.ParentSpanId;
        evt.DynamicSamplingContext = propagationContext.GetOrCreateDynamicSamplingContext(_options);
    }

    public SentryId CaptureEvent(SentryEvent evt, Action<Scope> configureScope)
        => CaptureEvent(evt, null, configureScope);

    public SentryId CaptureEvent(SentryEvent evt, Hint? hint, Action<Scope> configureScope)
    {
        if (!IsEnabled)
        {
            return SentryId.Empty;
        }

        try
        {
            var clonedScope = ScopeManager.GetCurrent().Key.Clone();
            configureScope(clonedScope);

            return CaptureEvent(evt, clonedScope, hint);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to capture event: {0}", evt.EventId);
            return SentryId.Empty;
        }
    }

    public SentryId CaptureEvent(SentryEvent evt, Scope? scope = null, Hint? hint = null)
    {
        if (!IsEnabled)
        {
            return SentryId.Empty;
        }

        try
        {
            ScopeManager.GetCurrent().Deconstruct(out var currentScope, out var sentryClient);
            var actualScope = scope ?? currentScope;

            // We get the span linked to the event or fall back to the current span
            var span = GetLinkedSpan(evt) ?? actualScope.Span;
            if (span is not null)
            {
                if (span.IsSampled is not false)
                {
                    ApplyTraceContextToEvent(evt, span);
                }
            }
            else
            {
                // If there is no span on the scope (and not just no sampled one), fall back to the propagation context
                ApplyTraceContextToEvent(evt, actualScope.PropagationContext);
            }

            // Now capture the event with the Sentry client on the current scope.
            var id = sentryClient.CaptureEvent(evt, actualScope, hint);
            actualScope.LastEventId = id;
            actualScope.SessionUpdate = null;

            if (evt.HasTerminalException() && actualScope.Transaction is { } transaction)
            {
                // Event contains a terminal exception -> finish any current transaction as aborted
                // Do this *after* the event was captured, so that the event is still linked to the transaction.
                _options.LogDebug("Ending transaction as Aborted, due to unhandled exception.");
                transaction.Finish(SpanStatus.Aborted);
            }

            return id;
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to capture event: {0}", evt.EventId);
            return SentryId.Empty;
        }
    }

    public void CaptureUserFeedback(UserFeedback userFeedback)
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            _ownedClient.CaptureUserFeedback(userFeedback);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to capture user feedback: {0}", userFeedback.EventId);
        }
    }

    public void CaptureTransaction(Transaction transaction) => CaptureTransaction(transaction, null, null);

    public void CaptureTransaction(Transaction transaction, Scope? scope, Hint? hint)
    {
        // Note: The hub should capture transactions even if it is disabled.
        // This allows transactions to be reported as failed when they encountered an unhandled exception,
        // in the case where the hub was disabled before the transaction was captured.
        // For example, that can happen with a top-level async main because IDisposables are processed before
        // the unhandled exception event fires.
        //
        // Any transactions started after the hub was disabled will already be sampled out and thus will
        // not be passed along to sentry when captured here.

        try
        {
            var (currentScope, client) = ScopeManager.GetCurrent();
            scope ??= currentScope;

            client.CaptureTransaction(transaction, scope, hint);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to capture transaction: {0}", transaction.SpanId);
        }
    }

    public void CaptureSession(SessionUpdate sessionUpdate)
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            _ownedClient.CaptureSession(sessionUpdate);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to capture session update: {0}", sessionUpdate.Id);
        }
    }

    public async Task FlushAsync(TimeSpan timeout)
    {
        try
        {
            var (_, client) = ScopeManager.GetCurrent();
            await client.FlushAsync(timeout).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to Flush events");
        }
    }

    public void Dispose()
    {
        _options.LogInfo("Disposing the Hub.");

        if (Interlocked.Exchange(ref _isEnabled, 0) != 1)
        {
            return;
        }

        _ownedClient.Flush(_options.ShutdownTimeout);
        //Dont dispose of ScopeManager since we want dangling transactions to still be able to access tags.
    }

    public SentryId LastEventId
    {
        get
        {
            var currentScope = ScopeManager.GetCurrent();
            return currentScope.Key.LastEventId;
        }
    }
}

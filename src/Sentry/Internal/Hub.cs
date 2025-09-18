using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Integrations;
using Sentry.Internal.Extensions;
using Sentry.Protocol.Envelopes;
using Sentry.Protocol.Metrics;

namespace Sentry.Internal;

internal class Hub : IHub, IDisposable
{
    private readonly object _sessionPauseLock = new();

    private readonly ISystemClock _clock;
    private readonly ISessionManager _sessionManager;
    private readonly SentryOptions _options;
    private readonly ISampleRandHelper _sampleRandHelper;
    private readonly RandomValuesFactory _randomValuesFactory;
    private readonly IReplaySession _replaySession;
    private readonly List<IDisposable> _integrationsToCleanup = new();

#if MEMORY_DUMP_SUPPORTED
    private readonly MemoryMonitor? _memoryMonitor;
#endif

    private int _isPersistedSessionRecovered;

    // Internal for testability
    internal ConditionalWeakTable<Exception, ISpan> ExceptionToSpanMap { get; } = new();

    internal IInternalScopeManager ScopeManager { get; }

    private int _isEnabled = 1;
    public bool IsEnabled => _isEnabled == 1;

    internal SentryOptions Options => _options;

    private Scope CurrentScope => ScopeManager.GetCurrent().Key;
    private ISentryClient CurrentClient => ScopeManager.GetCurrent().Value;

    internal Hub(
        SentryOptions options,
        ISentryClient? client = null,
        ISessionManager? sessionManager = null,
        ISystemClock? clock = null,
        IInternalScopeManager? scopeManager = null,
        RandomValuesFactory? randomValuesFactory = null,
        IReplaySession? replaySession = null,
        ISampleRandHelper? sampleRandHelper = null)
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
        _sessionManager = sessionManager ?? new GlobalSessionManager(options);
        _clock = clock ?? SystemClock.Clock;
        if (_options.EnableBackpressureHandling && _options.BackpressureMonitor is null)
        {
            _options.BackpressureMonitor = new BackpressureMonitor(_options.DiagnosticLogger, clock);
        }
        client ??= new SentryClient(options, randomValuesFactory: _randomValuesFactory, sessionManager: _sessionManager);
        _replaySession = replaySession ?? ReplaySession.Instance;
        _sampleRandHelper = sampleRandHelper ?? new SampleRandHelperAdapter();
        ScopeManager = scopeManager ?? new SentryScopeManager(options, client);

        if (!options.IsGlobalModeEnabled)
        {
            // Push the first scope so the async local starts from here
            PushScope();
        }

        Logger = SentryStructuredLogger.Create(this, options, _clock);

#if MEMORY_DUMP_SUPPORTED
        if (options.HeapDumpOptions is not null)
        {
            if (_options.DisableFileWrite)
            {
                _options.LogError("Automatic Heap Dumps cannot be used with file write disabled.");
            }
            else
            {
                _memoryMonitor = new MemoryMonitor(options, CaptureHeapDump);
            }
        }
#endif

        foreach (var integration in options.Integrations)
        {
            options.LogDebug("Registering integration: '{0}'.", integration.GetType().Name);
            integration.Register(this, options);
            if (integration is IDisposable disposableIntegration)
            {
                _integrationsToCleanup.Add(disposableIntegration);
            }
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

    public void ConfigureScope<TArg>(Action<Scope, TArg> configureScope, TArg arg)
    {
        try
        {
            ScopeManager.ConfigureScope(configureScope, arg);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to ConfigureScope<TArg>");
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

    public async Task ConfigureScopeAsync<TArg>(Func<Scope, TArg, Task> configureScope, TArg arg)
    {
        try
        {
            await ScopeManager.ConfigureScopeAsync(configureScope, arg).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to ConfigureScopeAsync<TArg>");
        }
    }

    public void SetTag(string key, string value) => ScopeManager.SetTag(key, value);

    public void UnsetTag(string key) => ScopeManager.UnsetTag(key);

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
        // If the hub is disabled, we will always sample out.  In other words, starting a transaction
        // after disposing the hub will result in that transaction not being sent to Sentry.
        if (!IsEnabled)
        {
            return NoOpTransaction.Instance;
        }

        bool? isSampled = null;
        double? sampleRate = null;
        DiscardReason? discardReason = null;
        var sampleRand = dynamicSamplingContext?.Items.TryGetValue("sample_rand", out var dscSampleRand) ?? false
            ? double.Parse(dscSampleRand, NumberStyles.Float, CultureInfo.InvariantCulture)
            : _sampleRandHelper.GenerateSampleRand(context.TraceId.ToString());

        // TracesSampler runs regardless of whether a decision has already been made, as it can be used to override it.
        if (_options.TracesSampler is { } tracesSampler)
        {
            var samplingContext = new TransactionSamplingContext(
                context,
                customSamplingContext);

            if (tracesSampler(samplingContext) is { } samplerSampleRate)
            {
                // The TracesSampler trumps all other sampling decisions (even the trace header)
                sampleRate = samplerSampleRate * _options.BackpressureMonitor.GetDownsampleFactor();
                isSampled = SampleRandHelper.IsSampled(sampleRand, sampleRate.Value);
                if (isSampled is false)
                {
                    // If sampling out is only a result of the downsampling then we specify the reason as backpressure
                    // management... otherwise the event would have been sampled out anyway, so it's just regular sampling.
                    discardReason = sampleRand < samplerSampleRate ? DiscardReason.Backpressure : DiscardReason.SampleRate;
                }

                // Ensure the actual sampleRate is set on the provided DSC (if any) when the TracesSampler reached a sampling decision
                dynamicSamplingContext?.SetSampleRate(samplerSampleRate);
            }
        }

        // If the sampling decision isn't made by a trace sampler we check the trace header first (from the context) or
        // finally fallback to Random sampling if the decision has been made by no other means
        if (isSampled == null)
        {
            var optionsSampleRate = _options.TracesSampleRate ?? 0.0;
            sampleRate = optionsSampleRate * _options.BackpressureMonitor.GetDownsampleFactor();
            isSampled = context.IsSampled ?? SampleRandHelper.IsSampled(sampleRand, sampleRate.Value);
            if (isSampled is false)
            {
                // If sampling out is only a result of the downsampling then we specify the reason as backpressure
                // management... otherwise the event would have been sampled out anyway, so it's just regular sampling.
                discardReason = sampleRand < optionsSampleRate ? DiscardReason.Backpressure : DiscardReason.SampleRate;
            }

            if (context.IsSampled is null && _options.TracesSampleRate is not null)
            {
                // Ensure the actual sampleRate is set on the provided DSC (if any) when not IsSampled upstream but the TracesSampleRate reached a sampling decision
                dynamicSamplingContext?.SetSampleRate(_options.TracesSampleRate.Value);
            }
        }

        // Make sure there is a replayId (if available) on the provided DSC (if any).
        dynamicSamplingContext?.SetReplayId(_replaySession);

        if (isSampled is false)
        {
            var unsampledTransaction = new UnsampledTransaction(this, context)
            {
                SampleRate = sampleRate,
                SampleRand = sampleRand,
                DiscardReason = discardReason,
                DynamicSamplingContext = dynamicSamplingContext // Default to the provided DSC
            };
            // If no DSC was provided, create one based on this transaction.
            // Must be done AFTER the sampling decision has been made (the DSC propagates sampling decisions).
            unsampledTransaction.DynamicSamplingContext ??= unsampledTransaction.CreateDynamicSamplingContext(_options, _replaySession);
            return unsampledTransaction;
        }

        var transaction = new TransactionTracer(this, context)
        {
            SampleRate = sampleRate,
            SampleRand = sampleRand,
            DynamicSamplingContext = dynamicSamplingContext // Default to the provided DSC
        };
        // If no DSC was provided, create one based on this transaction.
        // Must be done AFTER the sampling decision has been made (the DSC propagates sampling decisions).
        transaction.DynamicSamplingContext ??= transaction.CreateDynamicSamplingContext(_options, _replaySession);

        if (_options.TransactionProfilerFactory is { } profilerFactory &&
            _randomValuesFactory.NextBool(_options.ProfilesSampleRate ?? 0.0))
        {
            // TODO cancellation token based on Hub being closed?
            transaction.TransactionProfiler = profilerFactory.Start(transaction, CancellationToken.None);
        }

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

    public ISpan? GetSpan() => CurrentScope.Span;

    public SentryTraceHeader GetTraceHeader()
    {
        if (GetSpan()?.GetTraceHeader() is { } traceHeader)
        {
            return traceHeader;
        }

        // With either tracing disabled or no active span on the current scope we fall back to the propagation context
        var propagationContext = CurrentScope.PropagationContext;
        // In either case, we must not append a sampling decision.
        return new SentryTraceHeader(propagationContext.TraceId, propagationContext.SpanId, null);
    }

    public BaggageHeader GetBaggage()
    {
        var span = GetSpan();
        if (span?.GetTransaction().GetDynamicSamplingContext() is { IsEmpty: false } dsc)
        {
            return dsc.ToBaggageHeader();
        }

        var propagationContext = CurrentScope.PropagationContext;
        return propagationContext.GetOrCreateDynamicSamplingContext(_options, _replaySession).ToBaggageHeader();
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
        var propagationContext = SentryPropagationContext.CreateFromHeaders(_options.DiagnosticLogger, traceHeader, baggageHeader, _replaySession);
        ConfigureScope(static (scope, propagationContext) => scope.SetPropagationContext(propagationContext), propagationContext);

        return new TransactionContext(
            name: name ?? string.Empty,
            operation: operation ?? string.Empty,
            spanId: propagationContext.SpanId,
            parentSpanId: propagationContext.ParentSpanId,
            traceId: propagationContext.TraceId,
            isSampled: traceHeader?.IsSampled,
            isParentSampled: traceHeader?.IsSampled);
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

        if (span.GetTransaction().GetDynamicSamplingContext() is { } dsc)
        {
            evt.DynamicSamplingContext = dsc;
        }
    }

    private void ApplyTraceContextToEvent(SentryEvent evt, SentryPropagationContext propagationContext)
    {
        evt.Contexts.Trace.TraceId = propagationContext.TraceId;
        evt.Contexts.Trace.SpanId = propagationContext.SpanId;
        evt.Contexts.Trace.ParentSpanId = propagationContext.ParentSpanId;
        evt.DynamicSamplingContext = propagationContext.GetOrCreateDynamicSamplingContext(_options, _replaySession);
    }

    public bool CaptureEnvelope(Envelope envelope) => CurrentClient.CaptureEnvelope(envelope);

    private void AddBreadcrumbForException(SentryEvent evt, Scope scope)
    {
        try
        {
            if (!IsEnabled || evt.Exception is not { } exception)
            {
                return;
            }

            var exceptionMessage = exception.Message ?? "";
            var formatted = evt.Message?.Formatted;

            string breadcrumbMessage;
            Dictionary<string, string>? data = null;
            if (string.IsNullOrWhiteSpace(formatted))
            {
                breadcrumbMessage = exceptionMessage;
            }
            else
            {
                breadcrumbMessage = formatted;
                // Exception.Message won't be used as Breadcrumb message
                // Avoid losing it by adding as data:
                data = new Dictionary<string, string>
                {
                    {"exception_message", exceptionMessage}
                };
            }
            scope.AddBreadcrumb(breadcrumbMessage, "Exception", data: data, level: BreadcrumbLevel.Critical);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to store breadcrumb for exception event: {0}", evt.EventId);
        }
    }

    public SentryId CaptureEvent(SentryEvent evt, Action<Scope> configureScope)
        => CaptureEvent(evt, null, configureScope);

    public SentryId CaptureEvent(SentryEvent evt, SentryHint? hint, Action<Scope> configureScope)
    {
        if (!IsEnabled)
        {
            return SentryId.Empty;
        }

        try
        {
            var clonedScope = CurrentScope.Clone();
            configureScope(clonedScope);

            // Although we clone a temporary scope for the configureScope action, for the second scope
            // argument (the breadcrumbScope) we pass in the current scope... this is because we want
            // a breadcrumb to be left on the current scope for exception events
            var eventId = CaptureEvent(evt, hint, clonedScope);
            AddBreadcrumbForException(evt, CurrentScope);
            return eventId;
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to capture event: {0}", evt.EventId);
            return SentryId.Empty;
        }
    }

    public SentryId CaptureEvent(SentryEvent evt, Scope? scope = null, SentryHint? hint = null)
    {
        scope ??= CurrentScope;
        var eventId = CaptureEvent(evt, hint, scope);
        AddBreadcrumbForException(evt, scope);
        return eventId;
    }

    private SentryId CaptureEvent(SentryEvent evt, SentryHint? hint, Scope scope)
    {
        if (!IsEnabled)
        {
            return SentryId.Empty;
        }

        try
        {
            // We get the span linked to the event or fall back to the current span
            var span = GetLinkedSpan(evt) ?? scope.Span;
            if (span is not null)
            {
                ApplyTraceContextToEvent(evt, span);
            }
            else
            {
                // If there is no span on the scope (and not just no sampled one), fall back to the propagation context
                ApplyTraceContextToEvent(evt, scope.PropagationContext);
            }

            // Now capture the event with the Sentry client on the current scope.
            var id = CurrentClient.CaptureEvent(evt, scope, hint);
            scope.LastEventId = id;
            scope.SessionUpdate = null;

            if (evt.HasTerminalException() && scope.Transaction is { } transaction)
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

    public void CaptureFeedback(SentryFeedback feedback, Action<Scope> configureScope, SentryHint? hint = null)
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            var clonedScope = CurrentScope.Clone();
            configureScope(clonedScope);

            CaptureFeedback(feedback, clonedScope, hint);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to capture feedback");
        }
    }

    public void CaptureFeedback(SentryFeedback feedback, Scope? scope = null, SentryHint? hint = null)
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(feedback.ContactEmail) && !EmailValidator.IsValidEmail(feedback.ContactEmail))
            {
                _options.LogWarning("Feedback email scrubbed due to invalid email format: '{0}'", feedback.ContactEmail);
                feedback.ContactEmail = null;
            }

            scope ??= CurrentScope;
            CurrentClient.CaptureFeedback(feedback, scope, hint);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to capture feedback");
        }
    }

#if MEMORY_DUMP_SUPPORTED
    internal void CaptureHeapDump(string dumpFile)
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            _options.LogDebug("Capturing heap dump '{0}'", dumpFile);

            var evt = new SentryEvent
            {
                Message = "Memory threshold exceeded",
                Level = _options.HeapDumpOptions?.Level ?? SentryLevel.Warning,
            };
            var hint = new SentryHint(_options);
            hint.AddAttachment(dumpFile);
            CaptureEvent(evt, CurrentScope, hint);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to capture heap dump");
        }
    }
#endif

    [Obsolete("Use CaptureFeedback instead.")]
    public void CaptureUserFeedback(UserFeedback userFeedback)
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(userFeedback.Email) && !EmailValidator.IsValidEmail(userFeedback.Email))
            {
                _options.LogWarning("Feedback email scrubbed due to invalid email format: '{0}'", userFeedback.Email);
                userFeedback = new UserFeedback(
                    userFeedback.EventId,
                    userFeedback.Name,
                    null, // Scrubbed email
                    userFeedback.Comments);
            }

            CurrentClient.CaptureUserFeedback(userFeedback);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to capture user feedback: {0}", userFeedback.EventId);
        }
    }

    public void CaptureTransaction(SentryTransaction transaction) => CaptureTransaction(transaction, null, null);

    public void CaptureTransaction(SentryTransaction transaction, Scope? scope, SentryHint? hint)
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
            CurrentClient.CaptureTransaction(transaction, scope ?? CurrentScope, hint);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to capture transaction: {0}", transaction.SpanId);
        }
    }

    public void CaptureMetrics(IEnumerable<Metric> metrics)
    {
        if (!IsEnabled)
        {
            return;
        }

        Metric[]? enumerable = null;
        try
        {
            enumerable = metrics as Metric[] ?? metrics.ToArray();
            _options.LogDebug("Capturing metrics.");
            CurrentClient.CaptureEnvelope(Envelope.FromMetrics(metrics));
        }
        catch (Exception e)
        {
            var metricEventIds = enumerable?.Select(m => m.EventId).ToArray() ?? [];
            _options.LogError(e, "Failure to capture metrics: {0}", string.Join(",", metricEventIds));
        }
    }

    public void CaptureCodeLocations(CodeLocations codeLocations)
    {
        if (!IsEnabled)
        {
            return;
        }

        try
        {
            _options.LogDebug("Capturing code locations for period: {0}", codeLocations.Timestamp);
            CurrentClient.CaptureEnvelope(Envelope.FromCodeLocations(codeLocations));
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to capture code locations");
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
            CurrentClient.CaptureSession(sessionUpdate);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to capture session update: {0}", sessionUpdate.Id);
        }
    }

    public SentryId CaptureCheckIn(
        string monitorSlug,
        CheckInStatus status,
        SentryId? sentryId = null,
        TimeSpan? duration = null,
        Scope? scope = null,
        Action<SentryMonitorOptions>? configureMonitorOptions = null)
    {
        if (!IsEnabled)
        {
            return SentryId.Empty;
        }

        try
        {
            _options.LogDebug("Capturing '{0}' check-in for '{1}'", status, monitorSlug);

            scope ??= CurrentScope;
            return CurrentClient.CaptureCheckIn(monitorSlug, status, sentryId, duration, scope, configureMonitorOptions);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to capture check in for: {0}", monitorSlug);
        }

        return SentryId.Empty;
    }

    // Internal capture method that allows the Unity SDK to send attachments after an already captured event.
    // Kept internal as the preferred way of adding attachments is either on the scope or directly on the event.
    // See https://develop.sentry.dev/sdk/data-model/envelope-items/#attachment
    internal bool CaptureAttachment(SentryId eventId, SentryAttachment attachment)
    {
        if (!IsEnabled || eventId == SentryId.Empty || attachment.IsNull())
        {
            return false;
        }

        try
        {
            var envelope = Envelope.FromAttachment(eventId, attachment, _options.DiagnosticLogger);
            return CaptureEnvelope(envelope);
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failure to capture attachment");
            return false;
        }
    }

    public async Task FlushAsync(TimeSpan timeout)
    {
        try
        {
            Logger.Flush();
            await CurrentClient.FlushAsync(timeout).ConfigureAwait(false);
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

        foreach (var integration in _integrationsToCleanup)
        {
            try
            {
                integration.Dispose();
            }
            catch (Exception e)
            {
                _options.LogError("Failed to dispose integration {0}: {1}", integration.GetType().Name, e);
            }
        }

#if MEMORY_DUMP_SUPPORTED
        _memoryMonitor?.Dispose();
#endif

        Logger.Flush();
        (Logger as IDisposable)?.Dispose(); // see Sentry.Internal.DefaultSentryStructuredLogger

        try
        {
            CurrentClient.FlushAsync(_options.ShutdownTimeout).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to wait on disposing tasks to flush.");
        }
        //Don't dispose of ScopeManager since we want dangling transactions to still be able to access tags.

        if (_options.BackpressureMonitor is { } backpressureMonitor)
        {
            _options.BackpressureMonitor = null;
            backpressureMonitor.Dispose();
        }

#if __IOS__
            // TODO
#elif ANDROID
            // TODO
#elif NET8_0_OR_GREATER
        if (SentryNative.IsAvailable)
        {
            _options?.LogDebug("Closing native SDK");
            SentrySdk.CloseNativeSdk();
        }
#endif
    }

    public SentryId LastEventId => CurrentScope.LastEventId;

    public SentryStructuredLogger Logger { get; }
}

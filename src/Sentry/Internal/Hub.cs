using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Integrations;
using Sentry.Internal.ScopeStack;

namespace Sentry.Internal
{
    internal class Hub : IHub, IDisposable
    {
        private readonly object _sessionPauseLock = new();

        private readonly ISentryClient _ownedClient;
        private readonly ISystemClock _clock;
        private readonly ISessionManager _sessionManager;
        private readonly SentryOptions _options;
        private readonly ISdkIntegration[]? _integrations;
        private readonly IDisposable _rootScope;
        private readonly Enricher _enricher;

        private int _isPersistedSessionRecovered;

        // Internal for testability
        internal ConditionalWeakTable<Exception, ISpan> ExceptionToSpanMap { get; } = new();

        internal IInternalScopeManager ScopeManager { get; }

        private int _isEnabled = 1;
        public bool IsEnabled => _isEnabled == 1;

        internal Hub(
            SentryOptions options,
            ISentryClient? client = null,
            ISessionManager? sessionManager = null,
            ISystemClock? clock = null,
            IInternalScopeManager? scopeManager = null)
        {
            if (string.IsNullOrWhiteSpace(options.Dsn))
            {
                const string msg = "Attempt to instantiate a Hub without a DSN.";
                options.DiagnosticLogger?.LogFatal(msg);
                throw new InvalidOperationException(msg);
            }
            options.DiagnosticLogger?.LogDebug("Initializing Hub for Dsn: '{0}'.", options.Dsn);

            _options = options;
            _ownedClient = client ?? new SentryClient(options);
            _clock = clock ?? SystemClock.Clock;
            _sessionManager = sessionManager ?? new GlobalSessionManager(options);

            ScopeManager = scopeManager ?? new SentryScopeManager(
                options.ScopeStackContainer ?? new AsyncLocalScopeStackContainer(),
                options,
                _ownedClient
            );

            _rootScope = options.IsGlobalModeEnabled
                ? DisabledHub.Instance
                // Push the first scope so the async local starts from here
                : PushScope();

            _enricher = new Enricher(options);

            _integrations = options.Integrations;
            if (_integrations?.Length > 0)
            {
                foreach (var integration in _integrations)
                {
                    options.DiagnosticLogger?.LogDebug("Registering integration: '{0}'.", integration.GetType().Name);
                    integration.Register(this, options);
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
                _options.DiagnosticLogger?.LogError("Failure to ConfigureScope", e);
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
                _options.DiagnosticLogger?.LogError("Failure to ConfigureScopeAsync", e);
            }
        }

        public IDisposable PushScope() => ScopeManager.PushScope();

        public IDisposable PushScope<TState>(TState state) => ScopeManager.PushScope(state);

        public void WithScope(Action<Scope> scopeCallback)
        {
            try
            {
                ScopeManager.WithScope(scopeCallback);
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("Failure to run callback WithScope", e);
            }
        }

        public void BindClient(ISentryClient client) => ScopeManager.BindClient(client);

        public ITransaction StartTransaction(
            ITransactionContext context,
            IReadOnlyDictionary<string, object?> customSamplingContext)
        {
            var transaction = new TransactionTracer(this, context);

            // Tracing sampler callback runs regardless of whether a decision
            // has already been made, as it can be used to override it.
            if (_options.TracesSampler is { } tracesSampler)
            {
                var samplingContext = new TransactionSamplingContext(
                    context,
                    customSamplingContext
                );

                if (tracesSampler(samplingContext) is { } sampleRate)
                {
                    transaction.IsSampled = SynchronizedRandom.NextBool(sampleRate);
                }
            }

            // Random sampling runs only if the sampling decision hasn't
            // been made already.
            transaction.IsSampled ??= SynchronizedRandom.NextBool(_options.TracesSampleRate);

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

        public ISpan? GetSpan()
        {
            var (currentScope, _) = ScopeManager.GetCurrent();
            return currentScope.GetSpan();
        }

        public SentryTraceHeader? GetTraceHeader() => GetSpan()?.GetTraceHeader();

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
                    _options.DiagnosticLogger?.LogError(
                        "Failed to recover persisted session.",
                        ex
                    );
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
                _options.DiagnosticLogger?.LogError(
                    "Failed to start a session.",
                    ex
                );
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
                    _options.DiagnosticLogger?.LogError(
                        "Failed to pause a session.",
                        ex
                    );
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
                    _options.DiagnosticLogger?.LogError(
                        "Failed to resume a session.",
                        ex
                    );
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
                _options.DiagnosticLogger?.LogError(
                    "Failed to end a session.",
                    ex
                );
            }
        }

        public void EndSession(SessionEndStatus status = SessionEndStatus.Exited) =>
            EndSession(_clock.GetUtcNow(), status);

        private ISpan? GetLinkedSpan(SentryEvent evt, Scope scope)
        {
            // Find the span which is bound to the same exception
            if (evt.Exception is { } exception &&
                ExceptionToSpanMap.TryGetValue(exception, out var spanBoundToException))
            {
                return spanBoundToException;
            }

            // Otherwise just get the currently active span on the scope (unless it's sampled out)
            if (scope.GetSpan() is {IsSampled: not false} span)
            {
                return span;
            }

            return null;
        }

        public SentryId CaptureEvent(SentryEvent evt, Scope? scope = null)
        {
            try
            {
                var currentScope = ScopeManager.GetCurrent();
                var actualScope = scope ?? currentScope.Key;

                // Inject trace information from a linked span
                if (GetLinkedSpan(evt, actualScope) is { } linkedSpan)
                {
                    evt.Contexts.Trace.SpanId = linkedSpan.SpanId;
                    evt.Contexts.Trace.TraceId = linkedSpan.TraceId;
                    evt.Contexts.Trace.ParentSpanId = linkedSpan.ParentSpanId;
                }

                actualScope.SessionUpdate = evt switch
                {
                    // Event contains a terminal exception -> end session as crashed
                    var e when e.SentryExceptions?.Any(x => !(x.Mechanism?.Handled ?? true)) ?? false =>
                        _sessionManager.EndSession(SessionEndStatus.Crashed),

                    // Event contains a non-terminal exception -> report error
                    // (this might return null if the session has already reported errors before)
                    var e when e.Exception is not null || e.SentryExceptions?.Any() == true =>
                        _sessionManager.ReportError(),

                    // Event doesn't contain any kind of exception -> no reason to attach session update
                    _ => null
                };

                var id = currentScope.Value.CaptureEvent(evt, actualScope);
                actualScope.LastEventId = id;
                actualScope.SessionUpdate = null;

                return id;
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("Failure to capture event: {0}", e, evt.EventId);
                return SentryId.Empty;
            }
        }

        public void CaptureUserFeedback(UserFeedback userFeedback)
        {
            try
            {
                _ownedClient.CaptureUserFeedback(userFeedback);
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("Failure to capture user feedback: {0}", e, userFeedback.EventId);
            }
        }

        public void CaptureTransaction(Transaction transaction)
        {
            try
            {
                // Apply scope data
                var currentScope = ScopeManager.GetCurrent();
                currentScope.Key.Evaluate();
                currentScope.Key.Apply(transaction);

                // Apply enricher
                _enricher.Apply(transaction);

                currentScope.Value.CaptureTransaction(transaction);
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("Failure to capture transaction: {0}", e, transaction.SpanId);
            }
        }

        public void CaptureSession(SessionUpdate sessionUpdate)
        {
            try
            {
                _ownedClient.CaptureSession(sessionUpdate);
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("Failure to capture session update: {0}", e, sessionUpdate.Id);
            }
        }

        public async Task FlushAsync(TimeSpan timeout)
        {
            try
            {
                var currentScope = ScopeManager.GetCurrent();
                await currentScope.Value.FlushAsync(timeout).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("Failure to Flush events", e);
            }
        }

        public void Dispose()
        {
            _options.DiagnosticLogger?.LogInfo("Disposing the Hub.");

            if (Interlocked.Exchange(ref _isEnabled, 0) != 1)
            {
                return;
            }

            if (_integrations?.Length > 0)
            {
                foreach (var integration in _integrations)
                {
                    if (integration is IInternalSdkIntegration internalIntegration)
                    {
                        internalIntegration.Unregister(this);
                    }
                }
            }

            (_ownedClient as IDisposable)?.Dispose();
            _rootScope.Dispose();
            ScopeManager.Dispose();
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
}

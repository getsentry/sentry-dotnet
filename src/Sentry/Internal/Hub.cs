using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Internal
{
    internal class Hub : IHub, IDisposable
    {
        private readonly ISentryClient _ownedClient;
        private readonly ISessionManager _sessionManager;
        private readonly SentryOptions _options;
        private readonly ISdkIntegration[]? _integrations;
        private readonly IDisposable _rootScope;
        private readonly Enricher _enricher;

        private readonly ConditionalWeakTable<Exception, ISpan> _exceptionToSpanMap = new();

        internal SentryScopeManager ScopeManager { get; }

        private int _isEnabled = 1;
        public bool IsEnabled => _isEnabled == 1;

        internal Hub(ISentryClient client, ISessionManager sessionManager, SentryOptions options)
        {
            _ownedClient = client;
            _sessionManager = sessionManager;
            _options = options;

            if (Dsn.TryParse(options.Dsn) is null)
            {
                const string msg = "Attempt to instantiate a Hub without a DSN.";
                options.DiagnosticLogger?.LogFatal(msg);
                throw new InvalidOperationException(msg);
            }

            options.DiagnosticLogger?.LogDebug("Initializing Hub for Dsn: '{0}'.", options.Dsn);

            ScopeManager = new SentryScopeManager(options, _ownedClient);

            _integrations = options.Integrations;

            if (_integrations?.Length > 0)
            {
                foreach (var integration in _integrations)
                {
                    options.DiagnosticLogger?.LogDebug("Registering integration: '{0}'.", integration.GetType().Name);
                    integration.Register(this, options);
                }
            }

            // Push the first scope so the async local starts from here
            _rootScope = PushScope();

            _enricher = new Enricher(options);
        }

        internal Hub(ISentryClient client, SentryOptions options)
            : this(client, new GlobalSessionManager(options), options)
        {
        }

        public Hub(SentryOptions options)
            : this(new SentryClient(options), options)
        {
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
            _ = _exceptionToSpanMap.GetValue(exception, _ => span);
        }

        public ISpan? GetSpan()
        {
            var (currentScope, _) = ScopeManager.GetCurrent();
            return currentScope.GetSpan();
        }

        public SentryTraceHeader? GetTraceHeader() => GetSpan()?.GetTraceHeader();

        public void StartSession()
        {
            var session = _sessionManager.StartSession();
            if (session is not null)
            {
                ConfigureScope(scope => scope.Session = session);
                CaptureSession(session.CreateSnapshot(true));
            }
        }

        public void EndSession(SessionEndStatus status = SessionEndStatus.Exited)
        {
            var session = _sessionManager.EndSession(status);
            if (session is not null)
            {
                CaptureSession(session.CreateSnapshot(false));
            }
        }

        private ISpan? GetLinkedSpan(SentryEvent evt, Scope scope)
        {
            // Find the span which is bound to the same exception
            if (evt.Exception is { } exception &&
                _exceptionToSpanMap.TryGetValue(exception, out var spanBoundToException))
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

                // Treat all events as errors
                _sessionManager.ReportError();

                var id = currentScope.Value.CaptureEvent(evt, actualScope);
                actualScope.LastEventId = id;

                // If the event contains unhandled exception - end session as crashed
                if (evt.SentryExceptions?.Any(e => e.Mechanism?.Handled ?? true) ?? false)
                {
                    EndSession(SessionEndStatus.Crashed);
                }

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
                _options.DiagnosticLogger?.LogError("Failure to capture session snapshot: {0}", e, sessionUpdate.Session.Id);
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

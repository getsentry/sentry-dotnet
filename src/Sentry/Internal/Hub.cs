using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Internal
{
    internal class Hub : IHub, IDisposable
    {
        private readonly ISentryClient _ownedClient;
        private readonly SentryOptions _options;
        private readonly ISdkIntegration[]? _integrations;
        private readonly IDisposable _rootScope;
        private readonly Enricher _enricher;

        private readonly ConditionalWeakTable<Exception, ISpan> _exceptionToSpanMap = new();

        internal SentryScopeManager ScopeManager { get; }

        public bool IsEnabled => true;

        internal Hub(ISentryClient client, SentryOptions options)
        {
            _ownedClient = client;

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

        public ITransactionTracer StartTransaction(
            ITransactionContext context,
            IReadOnlyDictionary<string, object?> customSamplingContext)
        {
            var transaction = new TransactionTracer(this, context);

            // Transactions are not handled by event processors, so some things need to be added manually

            // Make a sampling decision if it hasn't been made already.
            // It could have been made by this point if the transaction was started
            // from a trace header which contains a sampling decision.
            if (transaction.IsSampled is null)
            {
                var samplingContext = new TransactionSamplingContext(
                    context,
                    customSamplingContext
                );

                var sampleRate =
                    // Custom sampler may not exist or may return null, in which case we fallback
                    // to the static sample rate.
                    _options.TracesSampler?.Invoke(samplingContext)
                    ?? _options.TracesSampleRate;

                transaction.IsSampled = sampleRate switch
                {
                    // Sample rate >= 1 means always sampled *in*
                    >= 1 => true,
                    // Sample rate <= 0 means always sampled *out*
                    <= 0 => false,
                    // Otherwise roll the dice
                    _ => SynchronizedRandom.NextDouble() < sampleRate
                };
            }

            // A sampled out transaction still appears fully functional to the user
            // but will be dropped by the client and won't reach Sentry's servers.

            // Sampling decision must have been made at this point
            Debug.Assert(transaction.IsSampled != null, "Started transaction without a sampling decision.");

            return transaction;
        }

        public void BindException(Exception exception, ISpan span)
        {
            // Don't overwrite existing pair in the unlikely event that it already exists
            _ = _exceptionToSpanMap.GetValue(exception, _ => span);
        }

        public ISpanTracer? GetSpan()
        {
            var (currentScope, _) = ScopeManager.GetCurrent();
            return currentScope.GetSpan();
        }

        public SentryTraceHeader? GetTraceHeader() => GetSpan()?.GetTraceHeader();

        public SentryId CaptureEvent(SentryEvent evt, Scope? scope = null)
        {
            try
            {
                var currentScope = ScopeManager.GetCurrent();
                var actualScope = scope ?? currentScope.Key;

                // Inject trace information from a linked span
                if (evt.Exception is { } exception &&
                    _exceptionToSpanMap.TryGetValue(exception, out var linkedSpan))
                {
                    evt.Contexts.Trace.SpanId = linkedSpan.SpanId;
                    evt.Contexts.Trace.TraceId = linkedSpan.TraceId;
                    evt.Contexts.Trace.ParentSpanId = linkedSpan.ParentSpanId;
                }

                var id = currentScope.Value.CaptureEvent(evt, actualScope);
                actualScope.LastEventId = id;

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

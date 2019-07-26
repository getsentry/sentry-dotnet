using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Integrations;
using Sentry.Protocol;

namespace Sentry.Internal
{
    internal class Hub : IHub, IDisposable
    {
        private readonly SentryOptions _options;
        private readonly ImmutableList<ISdkIntegration> _integrations;
        private readonly IDisposable _rootScope;

        private readonly SentryClient _ownedClient;

        internal SentryScopeManager ScopeManager { get; }

        public bool IsEnabled => true;

        public Hub(SentryOptions options)
        {
            Debug.Assert(options != null);
            _options = options;

            if (options.Dsn == null)
            {
                if (!Dsn.TryParse(DsnLocator.FindDsnStringOrDisable(), out var dsn))
                {
                    const string msg = "Attempt to instantiate a Hub without a DSN.";
                    options.DiagnosticLogger?.LogFatal(msg);
                    throw new InvalidOperationException(msg);
                }
                options.Dsn = dsn;
            }

            options.DiagnosticLogger?.LogDebug("Initializing Hub for Dsn: '{0}'.", options.Dsn);

            _ownedClient = new SentryClient(options);
            ScopeManager = new SentryScopeManager(options, _ownedClient);

            _integrations = options.Integrations;

            if (_integrations?.Count > 0)
            {
                foreach (var integration in _integrations)
                {
                    options.DiagnosticLogger?.LogDebug("Registering integration: '{0}'.", integration.GetType().Name);
                    integration.Register(this, options);
                }
            }

            // Push the first scope so the async local starts from here
            _rootScope = PushScope();
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

        public SentryId CaptureEvent(SentryEvent evt, Scope scope = null)
        {
            try
            {
                var (currentScope, client) = ScopeManager.GetCurrent();
                var actualScope = scope ?? currentScope;
                var id = client.CaptureEvent(evt, actualScope);
                actualScope.LastEventId = id;
                return id;
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("Failure to capture event: {0}", e, evt.EventId);
                return SentryId.Empty;
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
                _options.DiagnosticLogger?.LogError("Failure to Flush events", e);
            }
        }

        public void Dispose()
        {
            _options.DiagnosticLogger?.LogInfo("Disposing the Hub.");

            if (_integrations?.Count > 0)
            {
                foreach (var integration in _integrations)
                {
                    if (integration is IInternalSdkIntegration internalIntegration)
                    {
                        internalIntegration.Unregister(this);
                    }
                }
            }

            _ownedClient?.Dispose();
            _rootScope.Dispose();
            ScopeManager?.Dispose();
        }

        public SentryId LastEventId
        {
            get
            {
                var (currentScope, _) = ScopeManager.GetCurrent();
                return currentScope.LastEventId;
            }
        }
    }
}

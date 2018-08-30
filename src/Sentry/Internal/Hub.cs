using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Internal
{
    internal class Hub : IHub, IDisposable
    {
        private readonly SentryOptions _options;
        private readonly ImmutableList<ISdkIntegration> _integrations;

        public IInternalScopeManager ScopeManager { get; }
        private SentryClient _ownedClient;

        public bool IsEnabled => true;

        public Hub(SentryOptions options)
        {
            Debug.Assert(options != null);
            _options = options;

            if (options.Dsn == null)
            {
                const string msg = "Attempt to instantiate a Hub without a DSN.";
                options.DiagnosticLogger?.LogFatal(msg);
                throw new InvalidOperationException(msg);
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

        public void BindClient(ISentryClient client) => ScopeManager.BindClient(client);

        public Guid CaptureEvent(SentryEvent evt, Scope scope = null)
        {
            try
            {
                var (currentScope, client) = ScopeManager.GetCurrent();
                return client.CaptureEvent(evt, scope ?? currentScope);
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("Failure to capture event: {0}", e, evt.EventId);
                return Guid.Empty;
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
            _ownedClient = null;
            (ScopeManager as IDisposable)?.Dispose();
        }
    }
}

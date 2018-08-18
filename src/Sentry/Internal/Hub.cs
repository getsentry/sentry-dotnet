using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Integrations;
using Sentry.Protocol;

namespace Sentry.Internal
{
    // TODO Hub being the entry point, here's the place to capture
    // unhandled exceptions and notify via logging or callback
    internal class Hub : IHub, IDisposable
    {
        private readonly ImmutableList<ISdkIntegration> _integrations;

        public IInternalScopeManager ScopeManager { get; }
        private SentryClient _ownedClient;

        public bool IsEnabled => true;

        public Hub(SentryOptions options)
        {
            Debug.Assert(options != null);

            options.DiagnosticLogger?.LogDebug("Initializing Hub for Dsn: '{0}'.", options.Dsn);

            _ownedClient = new SentryClient(options);
            ScopeManager = new SentryScopeManager(options, _ownedClient);

            _integrations = options.Integrations;

            if (_integrations?.Count > 0)
            {
                foreach (var integration in _integrations)
                {
                    integration.Register(this);
                }
            }
        }

        public void ConfigureScope(Action<Scope> configureScope)
            => ScopeManager.ConfigureScope(configureScope);

        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope)
            => ScopeManager.ConfigureScopeAsync(configureScope);

        public IDisposable PushScope() => ScopeManager.PushScope();

        public IDisposable PushScope<TState>(TState state) => ScopeManager.PushScope(state);

        public void BindClient(ISentryClient client) => ScopeManager.BindClient(client);

        public Guid CaptureEvent(SentryEvent evt, Scope scope = null)
        {
            var (currentScope, client) = ScopeManager.GetCurrent();
            return client.CaptureEvent(evt, scope ?? currentScope);
        }

        public void Dispose()
        {
            if (_integrations?.Count > 0)
            {
                foreach (var integration in _integrations)
                {
                    integration.Unregister(this);
                }
            }

            _ownedClient?.Dispose();
            _ownedClient = null;
            (ScopeManager as IDisposable)?.Dispose();
        }
    }
}

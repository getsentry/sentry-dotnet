using System;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internal
{
    // TODO Hub being the entry point, here's the place to capture
    // unhandled exceptions and notify via logging or callback
    internal class Hub : IHub, IDisposable
    {
        public IInternalScopeManager ScopeManager { get; }
        private SentryClient _ownedClient;

        public bool IsEnabled => true;

        public Hub(SentryOptions options)
        {
            // Create client from options and bind
            _ownedClient = new SentryClient(options);
            ScopeManager = new SentryScopeManager(options, _ownedClient);

            // TODO: Subscribing or not should be based on the Options or some IIntegration
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // TODO: avoid stack overflow
            if (e.ExceptionObject is Exception ex)
            {
                // TODO: Add to Scope: Exception Mechanism = e.IsTerminating
                this.CaptureException(ex);
            }

            if (e.IsTerminating)
            {
                Dispose();
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
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
            _ownedClient?.Dispose();
            _ownedClient = null;
            (ScopeManager as IDisposable)?.Dispose();
        }
    }
}

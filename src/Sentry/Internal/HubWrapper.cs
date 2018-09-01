using System;
using System.Threading.Tasks;
using Sentry.Extensibility;

namespace Sentry.Internal
{
    // An enabled hub or not depending on Options
    internal class HubWrapper : IHub
    {
        private readonly IHub _hub;

        public bool IsEnabled => _hub.IsEnabled;

        public HubWrapper(SentryOptions options)
        {
            options.SetupLogging();

            if (options.Dsn == null)
            {
                if (!Dsn.TryParse(DsnLocator.FindDsnStringOrDisable(), out var dsn))
                {
                    options.DiagnosticLogger?.LogWarning("Init was called but no DSN was provided nor located. Sentry SDK will be disabled.");
                    _hub =  DisabledHub.Instance;
                    return;
                }
                options.Dsn = dsn;
            }

            _hub = new Hub(options);
        }

        public Guid CaptureEvent(SentryEvent evt, Scope scope = null) => _hub.CaptureEvent(evt, scope);

        public void ConfigureScope(Action<Scope> configureScope) => _hub.ConfigureScope(configureScope);

        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope) => _hub.ConfigureScopeAsync(configureScope);

        public void BindClient(ISentryClient client) => _hub.BindClient(client);

        public IDisposable PushScope() => _hub.PushScope();

        public IDisposable PushScope<TState>(TState state) => _hub.PushScope(state);
    }
}

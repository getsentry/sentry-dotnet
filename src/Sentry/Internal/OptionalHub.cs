using System;
using System.Threading.Tasks;
using Sentry.Extensibility;

namespace Sentry.Internal
{
    // Depending on Options:
    // - A proxy to a new Hub instance
    // or
    // - A proxy to SentrySdk which could hold a Hub if SentrySdk.Init was called, or a disabled Hub
    internal class OptionalHub : IHub, IDisposable
    {
        private readonly IHub _hub;
        private readonly IDisposable _disposable;

        public bool IsEnabled => _hub.IsEnabled;

        public OptionalHub(SentryOptions options)
        {
            options.SetupLogging();

            if (options.Dsn == null)
            {
                if (!Dsn.TryParse(DsnLocator.FindDsnStringOrDisable(), out var dsn))
                {
                    options.DiagnosticLogger?.LogWarning("Init was called but no DSN was provided nor located. Sentry SDK will be disabled.");
                    _hub = HubAdapter.Instance;
                    return;
                }
                options.Dsn = dsn;
            }

            _hub = new Hub(options);
            _disposable = SentrySdk.UseHub(_hub);
        }

        public Guid CaptureEvent(SentryEvent evt, Scope scope = null) => _hub.CaptureEvent(evt, scope);

        public void ConfigureScope(Action<Scope> configureScope) => _hub.ConfigureScope(configureScope);

        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope) => _hub.ConfigureScopeAsync(configureScope);

        public void BindClient(ISentryClient client) => _hub.BindClient(client);

        public IDisposable PushScope() => _hub.PushScope();

        public IDisposable PushScope<TState>(TState state) => _hub.PushScope(state);

        public void WithScope(Action<Scope> scopeCallback) => _hub.WithScope(scopeCallback);

        public Guid LastEventId => _hub.LastEventId;

        public void Dispose() => _disposable?.Dispose();
    }
}

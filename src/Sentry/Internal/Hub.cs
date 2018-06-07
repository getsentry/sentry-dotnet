using System;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internal
{
    // TODO Hub being the entry point, here's the place to capture
    // unhandled exceptions and notify via logging or callback
    internal class Hub : IHub
    {
        public IInternalScopeManagement ScopeManagement { get; }

        public bool IsEnabled => true;

        public Hub(SentryOptions options)
        {
            // Create client from options and bind
            var client = new SentryClient(options);
            ScopeManagement = new SentryScopeManagement(options, client);
        }

        public void ConfigureScope(Action<Scope> configureScope)
            => ScopeManagement.ConfigureScope(configureScope);

        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope)
            => ScopeManagement.ConfigureScopeAsync(configureScope);

        public IDisposable PushScope() => ScopeManagement.PushScope();

        public IDisposable PushScope<TState>(TState state) => ScopeManagement.PushScope(state);

        public void BindClient(ISentryClient client) => ScopeManagement.BindClient(client);

        public Guid CaptureEvent(SentryEvent evt, Scope scope = null)
        {
            var (currentScope, client) = ScopeManagement.GetCurrent();
            return client.CaptureEvent(evt, scope ?? currentScope);
        }
    }
}

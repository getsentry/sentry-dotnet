using System;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internal
{
    internal class DisabledHub : IHub, IDisposable
    {
        public static DisabledHub Instance = new DisabledHub();

        public bool IsEnabled => false;

        private DisabledHub() { }

        public void ConfigureScope(Action<Scope> configureScope) { }
        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope) => Task.CompletedTask;

        public IDisposable PushScope() => this;
        public IDisposable PushScope<TState>(TState state) => this;

        public void BindClient(ISentryClient client) { }

        public Guid CaptureEvent(SentryEvent evt, Scope scope = null) => Guid.Empty;

        public void Dispose() { }
    }
}

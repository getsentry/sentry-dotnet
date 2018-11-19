using System;
using System.Threading.Tasks;

namespace Sentry.Extensibility
{
    public class DisabledHub : IHub, IDisposable
    {
        public static DisabledHub Instance = new DisabledHub();

        public bool IsEnabled => false;

        private DisabledHub() { }

        public void ConfigureScope(Action<Scope> configureScope) { }
        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope) =>
#if NET45
            Task.FromResult(null as object);
#else
            Task.CompletedTask;
#endif

        public IDisposable PushScope() => this;
        public IDisposable PushScope<TState>(TState state) => this;

        public void WithScope(Action<Scope> scopeCallback) { }

        public void BindClient(ISentryClient client) { }

        public Guid CaptureEvent(SentryEvent evt, Scope scope = null) => Guid.Empty;

        public void Dispose() { }

        public Guid LastEventId => Guid.Empty;
    }
}

using System;
using System.Threading.Tasks;
using Sentry.Protocol;

namespace Sentry.Extensibility
{
    public class DisabledHub : IHub, IDisposable
    {
        public static DisabledHub Instance = new DisabledHub();

        public bool IsEnabled => false;

        private DisabledHub() { }

        public void ConfigureScope(Action<Scope> configureScope) { }
        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope) => Task.CompletedTask;

        public IDisposable PushScope() => this;
        public IDisposable PushScope<TState>(TState state) => this;

        public void WithScope(Action<Scope> scopeCallback) { }

        public void BindClient(ISentryClient client) { }

        public SentryId CaptureEvent(SentryEvent evt, Scope scope = null) => SentryId.Empty;

        public Task FlushAsync(TimeSpan timeout) => Task.CompletedTask;

        public void Dispose() { }

        public SentryId LastEventId => SentryId.Empty;
    }
}

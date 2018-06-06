using System;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internals
{
    internal sealed class DisabledSentryClient : ISentryClient, IDisposable
    {
        public static DisabledSentryClient Instance = new DisabledSentryClient();

        public bool IsEnabled => false;

        private DisabledSentryClient() { }

        public void ConfigureScope(Action<Scope> configureScope) { }
        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope) => Task.CompletedTask;

        public IDisposable PushScope() => this;
        public IDisposable PushScope<TState>(TState state) => this;

        public Guid CaptureEvent(SentryEvent evt) => Guid.Empty;
        public Guid CaptureEvent(Func<SentryEvent> eventFactory) => Guid.Empty;

        public void Dispose() { }
    }
}

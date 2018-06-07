using System;
using System.Threading.Tasks;
using Sentry.Internals;
using Sentry.Protocol;

namespace Sentry.Extensibility
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

        public Guid CaptureEvent(SentryEvent evt, Scope scope = null) => Guid.Empty;

        public void Dispose() { }
    }

    internal class Hub : IHub
    {
        public IInternalScopeManagement ScopeManagement { get; set; }

        public Hub(SentryOptions options)
        {
            ScopeManagement = new SentryScopeManagement(options);

            // Create client from options and bind
            ScopeManagement.BindClient();
        }

        public void ConfigureScope(Action<Scope> configureScope)
            => ScopeManagement.ConfigureScope(configureScope);

        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope)
            => ScopeManagement.ConfigureScopeAsync(configureScope);

        public IDisposable PushScope() => ScopeManagement.PushScope();

        public IDisposable PushScope<TState>(TState state) => ScopeManagement.PushScope(state);

        public bool IsEnabled { get; }

        public Guid CaptureEvent(SentryEvent evt, Scope scope = null)
        {
            throw new NotImplementedException();
        }

        public Guid CaptureEvent(Func<SentryEvent> eventFactory)
        {
            throw new NotImplementedException();
        }
    }
}

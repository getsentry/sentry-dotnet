using System;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Protocol;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Sentry
{
    internal class Sdk : ISdk
    {
        private readonly AsyncLocal<ImmutableStack<Scope>> _asyncLocalScope = new AsyncLocal<ImmutableStack<Scope>>();
        private readonly ISentryClient _client;

        internal ImmutableStack<Scope> ScopeStack
        {
            get => _asyncLocalScope.Value ?? (_asyncLocalScope.Value = ImmutableStack.Create(new Scope()));
            set => _asyncLocalScope.Value = value;
        }

        internal Scope Scope => ScopeStack.Peek();

        public Sdk(SentryOptions options)
        {
            // TODO: Subscribing or not should be based on the Options
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Create proper client based on Options
            _client = new HttpSentryClient(options);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                // TODO: Add to Scope: Exception Mechanism = e.IsTerminating
                CaptureException(ex);
            }
        }

        public void ConfigureScope(Action<Scope> configureScope)
        {
            var scope = ScopeStack.Peek();
            configureScope?.Invoke(scope);
        }

        // Microsoft.Extensions.Logging calls its equivalent method: BeginScope()
        public IDisposable PushScope()
        {
            var currentScopeStack = ScopeStack;
            var clonedScope = currentScopeStack.Peek().Clone();
            var scopeSnapshot = new ScopeSnapshot(currentScopeStack, this);
            ScopeStack = currentScopeStack.Push(clonedScope);

            return scopeSnapshot;
        }

        public SentryResponse CaptureEvent(SentryEvent evt)
            => WithClientAndScope((client, scope)
                => client.CaptureEvent(evt, scope));

        public SentryResponse CaptureException(Exception exception)
            => WithClientAndScope((client, scope)
                => client.CaptureException(exception, scope));

        public Task<SentryResponse> CaptureExceptionAsync(Exception exception)
            => WithClientAndScopeAsync((client, scope)
                => client.CaptureExceptionAsync(exception, scope));

        public SentryResponse WithClientAndScope(Func<ISentryClient, Scope, SentryResponse> handler)
            => handler(_client, ScopeStack.Peek());

        public Task<SentryResponse> WithClientAndScopeAsync(Func<ISentryClient, Scope, Task<SentryResponse>> handler)
            => handler(_client, Scope);

        public SentryResponse CaptureEvent(Func<SentryEvent> eventFactory)
            => _client?.CaptureEvent(eventFactory(), Scope);

        public async Task<SentryResponse> CaptureEventAsync(Func<Task<SentryEvent>> eventFactory)
        {
            // SDK enabled, invoke the factory and the client, asynchronously
            var @event = await eventFactory();
            return await _client.CaptureEventAsync(@event, Scope);
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;

            // Client should empty it's queue until SentryOptions.ShutdownTimeout
            (_client as IDisposable)?.Dispose();

            // TODO: set _isDisposed and throw ObjectDisposed from members
        }

        private class ScopeSnapshot : IDisposable
        {
            private readonly ImmutableStack<Scope> _snapshot;
            private readonly Sdk _sdk;

            public ScopeSnapshot(ImmutableStack<Scope> snapshot, Sdk sdk)
            {
                Debug.Assert(snapshot != null);
                Debug.Assert(sdk != null);
                _snapshot = snapshot;
                _sdk = sdk;
            }

            public void Dispose() => _sdk.ScopeStack = _snapshot;
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Protocol;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Sentry
{
    internal class Sdk : IDisposable
    {
        private readonly AsyncLocal<ImmutableStack<Scope>> _asyncLocalScope = new AsyncLocal<ImmutableStack<Scope>>();
        private ISentryClient _client;

        internal ImmutableStack<Scope> ScopeStack
        {
            get => _asyncLocalScope.Value ?? (_asyncLocalScope.Value = ImmutableStack.Create(new Scope()));
            set => _asyncLocalScope.Value = value;
        }

        internal Scope Scope => ScopeStack.Peek();

        public Sdk(SentryOptions options)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Create proper client based on Options
            _client = new HttpSentryClient(options);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            CaptureException(e.ExceptionObject as Exception);
        }

        internal void ConfigureScope(Action<Scope> configureScope)
        {
            if (_client != null)
            {
                var scope = ScopeStack.Peek();
                configureScope?.Invoke(scope);
            }
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
        {
            var client = _client;
            if (client == null)
            {
                // some Response object could always be returned while signaling SDK disabled instead of relying on magic strings
                return SentryResponse.Disabled;
            }

            return handler(client, ScopeStack.Peek());
        }

        public Task<SentryResponse> WithClientAndScopeAsync(Func<ISentryClient, Scope, Task<SentryResponse>> handler)
        {
            var client = _client;
            if (client == null)
            {
                // TODO: Task could be cached
                return Task.FromResult(SentryResponse.Disabled);
            }

            return handler(client, Scope);
        }

        public SentryResponse CaptureEvent(Func<SentryEvent> eventFactory)
            => _client?.CaptureEvent(eventFactory(), Scope);

        public async Task<SentryResponse> CaptureEventAsync(Func<Task<SentryEvent>> eventFactory)
        {
            var client = _client;
            if (client == null)
            {
                // Runs synchronously
                return SentryResponse.Disabled;
            }

            // SDK enabled, invoke the factory and the client, asynchronously
            var @event = await eventFactory();
            return await client.CaptureEventAsync(@event, Scope);
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

        public void Dispose()
        {
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;

            // Client should empty it's queue until SentryOptions.ShutdownTimeout
            _client.SafeDispose();
        }
    }
}

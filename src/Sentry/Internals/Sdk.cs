using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internals
{
    internal class Sdk : ISdk, IDisposable
    {
        private readonly ISentryClient _client;

        // Testability
        internal IInternalScopeManagement ScopeManagement { get; }

        public Sdk(SentryOptions options)
        {
            // TODO: Subscribing or not should be based on the Options
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Create proper client based on Options
            _client = new HttpSentryClient(options);
            ScopeManagement = new SentryScopeManagement();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // TODO: avoid stackoverflow
            if (e.ExceptionObject is Exception ex)
            {
                // TODO: Add to Scope: Exception Mechanism = e.IsTerminating
                CaptureException(ex);
            }
        }

        public bool IsEnabled => true;

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
            => handler(_client, ScopeManagement.GetCurrent());

        public Task<SentryResponse> WithClientAndScopeAsync(Func<ISentryClient, Scope, Task<SentryResponse>> handler)
            => handler(_client, ScopeManagement.GetCurrent());

        public SentryResponse CaptureEvent(Func<SentryEvent> eventFactory)
            => _client?.CaptureEvent(eventFactory(), ScopeManagement.GetCurrent());

        public async Task<SentryResponse> CaptureEventAsync(Func<Task<SentryEvent>> eventFactory)
        {
            // SDK enabled, invoke the factory and the client, asynchronously
            var @event = await eventFactory();
            return await _client.CaptureEventAsync(@event, ScopeManagement.GetCurrent());
        }

        [DebuggerStepThrough]
        public void ConfigureScope(Action<Scope> configureScope) => ScopeManagement.ConfigureScope(configureScope);

        [DebuggerStepThrough]
        public IDisposable PushScope() => ScopeManagement.PushScope();

        public void Dispose()
        {
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;

            // Client should empty it's queue until SentryOptions.ShutdownTimeout
            (_client as IDisposable)?.Dispose();

            // TODO: set _isDisposed and throw ObjectDisposed from members
        }
    }
}

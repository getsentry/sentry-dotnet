using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Infrastructure;
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
            ScopeManagement = new SentryScopeManagement(options);
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

        public void ConfigureScope(Action<Scope> configureScope) => ScopeManagement.ConfigureScope(configureScope);

        public IDisposable PushScope() => ScopeManagement.PushScope();

        public IDisposable PushScope<TState>(TState state) => ScopeManagement.PushScope(state);

        public void AddBreadcrumb(
            string message,
            string type,
            string category = null,
            IDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
            => AddBreadcrumb(
                    clock: null,
                    message: message,
                    type: type,
                    data: data?.ToImmutableDictionary(),
                    category: category,
                    level: level);

        public void AddBreadcrumb(ISystemClock clock, string message, string type = null, string category = null,
            IDictionary<string, string> data = null, BreadcrumbLevel level = default)
            => ConfigureScope(
                s => s.AddBreadcrumb(new Breadcrumb(
                    timestamp: (clock ?? SystemClock.Clock).GetUtcNow(),
                    message: message,
                    type: type,
                    data: data?.ToImmutableDictionary(),
                    category: category,
                    level: level)));

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

        public void Dispose()
        {
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;

            // Client should empty it's queue until SentryOptions.ShutdownTimeout
            (_client as IDisposable)?.Dispose();

            // TODO: set _isDisposed and throw ObjectDisposed from members
        }
    }
}

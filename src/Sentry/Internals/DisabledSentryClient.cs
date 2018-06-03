using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Protocol;

namespace Sentry.Internals
{
    internal sealed class DisabledSentryClient : ISentryClient, IDisposable
    {
        private static SentryResponse DisabledResponse { get; } = new SentryResponse(false, errorMessage: "SDK Disabled");
        private static readonly Task<SentryResponse> DisabledResponseTask = Task.FromResult(DisabledResponse);

        public static DisabledSentryClient Disabled = new DisabledSentryClient();

        private DisabledSentryClient() { }

        public void ConfigureScope(Action<Scope> configureScope) { }
        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope) => Task.CompletedTask;

        public IDisposable PushScope() => this;
        public IDisposable PushScope<TState>(TState state) => this;

        public void AddBreadcrumb(
            string message,
            string type,
            string category = null,
            IDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
        { }

        public void AddBreadcrumb(
            ISystemClock clock,
            string message,
            string type = null,
            string category = null,
            IDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
        { }

        public bool IsEnabled => false;

        public SentryResponse CaptureEvent(SentryEvent evt) => DisabledResponse;

        public Task<SentryResponse> CaptureEventAsync(SentryEvent evt) => DisabledResponseTask;

        public SentryResponse CaptureException(Exception exception) => DisabledResponse;

        public Task<SentryResponse> CaptureExceptionAsync(Exception exception) => DisabledResponseTask;

        public SentryResponse WithClientAndScope(Func<ISentryClient, Scope, SentryResponse> handler) => DisabledResponse;

        public Task<SentryResponse> WithClientAndScopeAsync(Func<ISentryClient, Scope, Task<SentryResponse>> handler) => DisabledResponseTask;

        public SentryResponse CaptureEvent(Func<SentryEvent> eventFactory) => DisabledResponse;

        public Task<SentryResponse> CaptureEventAsync(Func<Task<SentryEvent>> eventFactory) => DisabledResponseTask;

        public void Dispose() { }
    }
}

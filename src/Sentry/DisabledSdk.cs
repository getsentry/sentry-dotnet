using System;
using System.Threading.Tasks;
using Sentry.Protocol;

namespace Sentry
{
    internal sealed class DisabledSdk : ISdk
    {
        private static SentryResponse DisabledResponse { get; } = new SentryResponse(false, errorMessage: "SDK Disabled");
        private static readonly Task<SentryResponse> DisabledResponseTask = Task.FromResult(DisabledResponse);

        public static DisabledSdk Disabled = new DisabledSdk();

        private DisabledSdk() { }

        public void ConfigureScope(Action<Scope> configureScope) { }

        public IDisposable PushScope() => this;

        public SentryResponse CaptureEvent(SentryEvent evt) => DisabledResponse;

        public SentryResponse CaptureException(Exception exception) => DisabledResponse;

        public Task<SentryResponse> CaptureExceptionAsync(Exception exception) => DisabledResponseTask;

        public SentryResponse WithClientAndScope(Func<ISentryClient, Scope, SentryResponse> handler) => DisabledResponse;

        public Task<SentryResponse> WithClientAndScopeAsync(Func<ISentryClient, Scope, Task<SentryResponse>> handler) => DisabledResponseTask;

        public SentryResponse CaptureEvent(Func<SentryEvent> eventFactory) => DisabledResponse;

        public Task<SentryResponse> CaptureEventAsync(Func<Task<SentryEvent>> eventFactory) => DisabledResponseTask;

        public void Dispose() { }
    }
}

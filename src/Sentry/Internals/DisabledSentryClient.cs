using System;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internals
{
    internal sealed class DisabledSentryClient : ISentryClient, IDisposable
    {
        private static SentryResponse DisabledResponse { get; } = new SentryResponse(false, errorMessage: "SDK Disabled");
        private static readonly Task<SentryResponse> DisabledResponseTask = Task.FromResult(DisabledResponse);

        public static DisabledSentryClient Disabled = new DisabledSentryClient();

        public bool IsEnabled => false;

        private DisabledSentryClient() { }

        public void ConfigureScope(Action<Scope> configureScope) { }
        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope) => Task.CompletedTask;

        public IDisposable PushScope() => this;
        public IDisposable PushScope<TState>(TState state) => this;

        public SentryResponse CaptureEvent(SentryEvent evt) => DisabledResponse;
        public Task<SentryResponse> CaptureEventAsync(SentryEvent evt) => DisabledResponseTask;
        public SentryResponse CaptureEvent(Func<SentryEvent> eventFactory) => DisabledResponse;
        public Task<SentryResponse> CaptureEventAsync(Func<Task<SentryEvent>> eventFactory) => DisabledResponseTask;

        public void Dispose() { }
    }
}

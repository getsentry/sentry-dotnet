using System;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Protocol;
using System.Diagnostics;

namespace Sentry
{
    // Façade to the SDK instance
    [DebuggerStepThrough]
    public static class SentryCore
    {
        // TODO: At this point no Scope (e.g: breadcrumb) will be kept until the SDK is enabled
        private static ISdk _sdk = DisabledSdk.Disabled;

        public static void Init(Action<SentryOptions> configureOptions = null)
        {
            var options = new SentryOptions();
            configureOptions?.Invoke(options);

            var sdk = Interlocked.Exchange(ref _sdk, new Sdk(options));
            sdk?.Dispose(); // Possibily disposes an old client
        }

        public static void CloseAndFlush()
        {
            var sdk = Interlocked.Exchange(ref _sdk, DisabledSdk.Disabled);
            sdk?.Dispose(); // Possibily disposes an old client
        }

        public static bool IsEnabled = _sdk != DisabledSdk.Disabled;

        public static IDisposable PushScope() => _sdk?.PushScope();

        public static void ConfigureScope(Action<Scope> configureScope)
            => _sdk.ConfigureScope(configureScope);

        public static SentryResponse CaptureEvent(SentryEvent evt)
            => _sdk.CaptureEvent(evt);

        public static SentryResponse CaptureException(Exception exception)
            => _sdk.CaptureException(exception);

        public static Task<SentryResponse> CaptureExceptionAsync(Exception exception)
            => _sdk.CaptureExceptionAsync(exception);

        public static SentryResponse WithClientAndScope(Func<ISentryClient, Scope, SentryResponse> handler)
            => _sdk.WithClientAndScope(handler);

        public static Task<SentryResponse> WithClientAndScopeAsync(Func<ISentryClient, Scope, Task<SentryResponse>> handler)
            => _sdk.WithClientAndScopeAsync(handler);

        public static SentryResponse CaptureEvent(Func<SentryEvent> eventFactory)
            => _sdk.CaptureEvent(eventFactory);

        public static Task<SentryResponse> CaptureEventAsync(Func<Task<SentryEvent>> eventFactory)
            => _sdk.CaptureEventAsync(eventFactory);
    }
}

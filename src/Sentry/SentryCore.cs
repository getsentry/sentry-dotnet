using System;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Protocol;
using System.Diagnostics;
using Sentry.Extensibility;
using Sentry.Internals;

namespace Sentry
{
    /// <summary>
    /// Sentry SDK entrypoint
    /// </summary>
    /// <remarks>
    /// This is a façade to the SDK instance.
    /// It allows safe static access to a client and scope management.
    /// When the SDK is uninitialized, calls to this class result in no-op so no callbacks are invoked.
    /// </remarks>
    public static class SentryCore
    {
        // TODO: At this point no Scope (e.g: breadcrumb) will be kept until the SDK is enabled
        private static ISdk _sdk = DisabledSdk.Disabled;

        /// <summary>
        /// Initializes the SDK with the specified configuration options callback.
        /// </summary>
        /// <param name="configureOptions">The configure options.</param>
        public static void Init(Action<SentryOptions> configureOptions = null)
        {
            var options = new SentryOptions();
            configureOptions?.Invoke(options);

            var sdk = Interlocked.Exchange(ref _sdk, new Sdk(options));
            (sdk as IDisposable)?.Dispose(); // Possibily disposes an old client
        }

        /// <summary>
        /// Closes the SDK and flushes any queued event to Sentry
        /// </summary>
        public static void CloseAndFlush()
        {
            var sdk = Interlocked.Exchange(ref _sdk, DisabledSdk.Disabled);
            (sdk as IDisposable)?.Dispose(); // Possibily disposes an old client
        }

        /// <summary>
        /// Whether the SDK is enabled or not
        /// </summary>
        public static bool IsEnabled { [DebuggerStepThrough] get => _sdk.IsEnabled; }

        /// <summary>
        /// Creates a new scope that will terminate when disposed
        /// </summary>
        /// <returns>A disposable that when disposed, ends the created scope.</returns>
        [DebuggerStepThrough]
        public static IDisposable PushScope() => _sdk?.PushScope();

        /// <summary>
        /// Configures the scope through the callback.
        /// </summary>
        /// <param name="configureScope">The configure scope.</param>
        [DebuggerStepThrough]
        public static void ConfigureScope(Action<Scope> configureScope)
            => _sdk.ConfigureScope(configureScope);

        /// <summary>
        /// Captures the event.
        /// </summary>
        /// <param name="evt">The evt.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static SentryResponse CaptureEvent(SentryEvent evt)
            => _sdk.CaptureEvent(evt);

        /// <summary>
        /// Captures the event.
        /// </summary>
        /// <param name="eventFactory">The event factory.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static SentryResponse CaptureEvent(Func<SentryEvent> eventFactory)
            => _sdk.CaptureEvent(eventFactory);

        /// <summary>
        /// Captures the event asynchronously.
        /// </summary>
        /// <param name="eventFactory">The event factory.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static Task<SentryResponse> CaptureEventAsync(Func<Task<SentryEvent>> eventFactory)
            => _sdk.CaptureEventAsync(eventFactory);

        /// <summary>
        /// Captures the exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static SentryResponse CaptureException(Exception exception)
            => _sdk.CaptureException(exception);

        /// <summary>
        /// Captures the exception asynchronously.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static Task<SentryResponse> CaptureExceptionAsync(Exception exception)
            => _sdk.CaptureExceptionAsync(exception);

        /// <summary>
        /// Provides the current client and scope to callback.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static SentryResponse WithClientAndScope(Func<ISentryClient, Scope, SentryResponse> handler)
            => _sdk.WithClientAndScope(handler);

        /// <summary>
        /// Provides the current client and scope to callback asynchronously.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static Task<SentryResponse> WithClientAndScopeAsync(Func<ISentryClient, Scope, Task<SentryResponse>> handler)
            => _sdk.WithClientAndScopeAsync(handler);
    }
}

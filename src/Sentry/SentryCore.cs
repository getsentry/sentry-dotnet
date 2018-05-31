using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Protocol;
using System.Diagnostics;
using Sentry.Extensibility;
using Sentry.Infrastructure;
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
        /// Initializes the SDK with an optional configuration options callback.
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
        /// <remarks>
        /// Pushes a new scope while inheriting the current scope's data.
        /// </remarks>
        /// <typeparam name="TState"></typeparam>
        /// <param name="state">A state object to be added to the scope</param>
        /// <returns>A disposable that when disposed, ends the created scope.</returns>
        [DebuggerStepThrough]
        public static IDisposable PushScope<TState>(TState state) => _sdk.PushScope(state);

        /// <summary>
        /// Creates a new scope that will terminate when disposed
        /// </summary>
        /// <returns>A disposable that when disposed, ends the created scope.</returns>
        [DebuggerStepThrough]
        public static IDisposable PushScope() => _sdk?.PushScope();

        /// <summary>
        /// Adds a breadcrumb to the current Scope
        /// </summary>
        /// <param name="message">
        /// If a message is provided it’s rendered as text and the whitespace is preserved.
        /// Very long text might be abbreviated in the UI.</param>
        /// <param name="type">
        /// The type of breadcrumb.
        /// The default type is default which indicates no specific handling.
        /// Other types are currently http for HTTP requests and navigation for navigation events.
        /// <seealso href="https://docs.sentry.io/clientdev/interfaces/breadcrumbs/#breadcrumb-types"/>
        /// </param>
        /// <param name="category">
        /// Categories are dotted strings that indicate what the crumb is or where it comes from.
        /// Typically it’s a module name or a descriptive string.
        /// For instance ui.click could be used to indicate that a click happend in the UI or flask could be used to indicate that the event originated in the Flask framework.
        /// </param>
        /// <param name="data">
        /// Data associated with this breadcrumb.
        /// Contains a sub-object whose contents depend on the breadcrumb type.
        /// Additional parameters that are unsupported by the type are rendered as a key/value table.
        /// </param>
        /// <param name="level">Breadcrumb level.</param>
        /// <seealso href="https://docs.sentry.io/clientdev/interfaces/breadcrumbs/"/>
        [DebuggerStepThrough]
        public static void AddBreadcrumb(
            string message,
            string type,
            string category = null,
            IDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
            => _sdk?.AddBreadcrumb(message, type, category, data, level);

        /// <summary>
        /// Adds a breadcrumb to the current scope
        /// </summary>
        /// <remarks>
        /// This overload is inteded to be used by integrations only.
        /// The objective is to allow better testability by allowing control of the timestamp set to the breadcrumb.
        /// </remarks>
        /// <param name="clock">An optional <see cref="ISystemClock"/></param>
        /// <param name="message">The message.</param>
        /// <param name="type">The type.</param>
        /// <param name="category">The category.</param>
        /// <param name="data">The data.</param>
        /// <param name="level">The level.</param>
        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddBreadcrumb(
            ISystemClock clock,
            string message,
            string type,
            string category = null,
            IDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
            => _sdk?.AddBreadcrumb(clock, message, type, category, data, level);

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
        /// Provides the current client and scope to the callback.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static SentryResponse WithClientAndScope(Func<ISentryClient, Scope, SentryResponse> handler)
            => _sdk.WithClientAndScope(handler);

        /// <summary>
        /// Provides the current client and scope to the callback asynchronously.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        public static Task<SentryResponse> WithClientAndScopeAsync(Func<ISentryClient, Scope, Task<SentryResponse>> handler)
            => _sdk.WithClientAndScopeAsync(handler);
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;

namespace Sentry
{
    /// <summary>
    /// Sentry SDK entrypoint.
    /// </summary>
    /// <remarks>
    /// This is a facade to the SDK instance.
    /// It allows safe static access to a client and scope management.
    /// When the SDK is uninitialized, calls to this class result in no-op so no callbacks are invoked.
    /// </remarks>
#if __MOBILE__
    public static partial class SentrySdk
#else
    public static class SentrySdk
#endif
    {
        private static IHub CurrentHub = DisabledHub.Instance;

        internal static SentryOptions? CurrentOptions => CurrentHub.GetSentryOptions();

        /// <summary>
        /// Last event id recorded in the current scope.
        /// </summary>
        public static SentryId LastEventId { [DebuggerStepThrough] get => CurrentHub.LastEventId; }

        internal static IHub InitHub(SentryOptions options)
        {
            options.SetupLogging();

            ProcessInfo.Instance ??= new ProcessInfo(options);

            // Locate the DSN
            var dsnString = options.SettingLocator.GetDsn();

            // If it's either explicitly disabled or we couldn't resolve the DSN
            // from anywhere else, return a disabled hub.
            if (Dsn.IsDisabled(dsnString))
            {
                options.LogWarning("Init was called but no DSN was provided nor located. Sentry SDK will be disabled.");
                return DisabledHub.Instance;
            }

            // Validate DSN for an early exception in case it's malformed
            var dsn = Dsn.Parse(dsnString);
            if (dsn.SecretKey != null)
            {
                options.LogWarning("The provided DSN that contains a secret key. This is not required and will be ignored.");
            }

            // Initialize bundled platform SDKs here
#if ANDROID
            InitSentryAndroidSdk(options);
#elif __IOS__
            InitSentryCocoaSdk(options);
#endif
            return new Hub(options);
        }

        /// <summary>
        /// Initializes the SDK while attempting to locate the DSN.
        /// </summary>
        /// <remarks>
        /// If the DSN is not found, the SDK will not change state.
        /// </remarks>
        /// <returns>An object that should be disposed when the application terminates.</returns>
        public static IDisposable Init() => Init((string?)null);

        /// <summary>
        /// Initializes the SDK with the specified DSN.
        /// </summary>
        /// <remarks>
        /// An empty string is interpreted as a disabled SDK.
        /// </remarks>
        /// <seealso href="https://develop.sentry.dev/sdk/overview/#usage-for-end-users"/>
        /// <param name="dsn">The dsn.</param>
        /// <returns>An object that should be disposed when the application terminates.</returns>
        public static IDisposable Init(string? dsn) => !Dsn.IsDisabled(dsn)
            ? Init(c => c.Dsn = dsn)
            : DisabledHub.Instance;

        /// <summary>
        /// Initializes the SDK with an optional configuration options callback.
        /// </summary>
        /// <param name="configureOptions">The configuration options callback.</param>
        /// <returns>An object that should be disposed when the application terminates.</returns>
        public static IDisposable Init(Action<SentryOptions>? configureOptions)
        {
            var options = new SentryOptions();
            configureOptions?.Invoke(options);

            return Init(options);
        }

        /// <summary>
        /// Initializes the SDK with the specified options instance.
        /// </summary>
        /// <param name="options">The options instance</param>
        /// <remarks>
        /// Used by integrations which have their own delegates.
        /// </remarks>
        /// <returns>An object that should be disposed when the application terminates.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IDisposable Init(SentryOptions options) => UseHub(InitHub(options));

        internal static IDisposable UseHub(IHub hub)
        {
            var oldHub = Interlocked.Exchange(ref CurrentHub, hub);
            (oldHub as IDisposable)?.Dispose();
            return new DisposeHandle(hub);
        }

        /// <summary>
        /// Flushes events queued up.
        /// </summary>
        [DebuggerStepThrough]
        public static Task FlushAsync(TimeSpan timeout) => CurrentHub.FlushAsync(timeout);

        /// <summary>
        /// Close the SDK.
        /// </summary>
        /// <remarks>
        /// Flushes the events and disables the SDK.
        /// This method is mostly used for testing the library since
        /// Init returns a IDisposable that can be used to shutdown the SDK.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Close()
        {
            var oldHub = Interlocked.Exchange(ref CurrentHub, DisabledHub.Instance);
            (oldHub as IDisposable)?.Dispose();
            ProcessInfo.Instance = null;
        }

        private class DisposeHandle : IDisposable
        {
            private IHub _localHub;
            public DisposeHandle(IHub hub) => _localHub = hub;

            public void Dispose()
            {
                _ = Interlocked.CompareExchange(ref CurrentHub, DisabledHub.Instance, _localHub);
                (_localHub as IDisposable)?.Dispose();

                _localHub = null!;
            }
        }

        /// <summary>
        /// Whether the SDK is enabled or not.
        /// </summary>
        public static bool IsEnabled { [DebuggerStepThrough] get => CurrentHub.IsEnabled; }

        /// <summary>
        /// Creates a new scope that will terminate when disposed.
        /// </summary>
        /// <remarks>
        /// Pushes a new scope while inheriting the current scope's data.
        /// </remarks>
        /// <param name="state">A state object to be added to the scope.</param>
        /// <returns>A disposable that when disposed, ends the created scope.</returns>
        [DebuggerStepThrough]
        public static IDisposable PushScope<TState>(TState state) => CurrentHub.PushScope(state);

        /// <summary>
        /// Creates a new scope that will terminate when disposed.
        /// </summary>
        /// <returns>A disposable that when disposed, ends the created scope.</returns>
        [DebuggerStepThrough]
        public static IDisposable PushScope() => CurrentHub.PushScope();

        /// <summary>
        /// Binds the client to the current scope.
        /// </summary>
        /// <param name="client">The client.</param>
        [DebuggerStepThrough]
        public static void BindClient(ISentryClient client) => CurrentHub.BindClient(client);

        /// <summary>
        /// Adds a breadcrumb to the current Scope.
        /// </summary>
        /// <param name="message">
        /// If a message is provided it’s rendered as text and the whitespace is preserved.
        /// Very long text might be abbreviated in the UI.</param>
        /// <param name="category">
        /// Categories are dotted strings that indicate what the crumb is or where it comes from.
        /// Typically it’s a module name or a descriptive string.
        /// For instance ui.click could be used to indicate that a click happened in the UI or flask could be used to indicate that the event originated in the Flask framework.
        /// </param>
        /// <param name="type">
        /// The type of breadcrumb.
        /// The default type is default which indicates no specific handling.
        /// Other types are currently http for HTTP requests and navigation for navigation events.
        /// <seealso href="https://develop.sentry.dev/sdk/event-payloads/breadcrumbs/#breadcrumb-types"/>
        /// </param>
        /// <param name="data">
        /// Data associated with this breadcrumb.
        /// Contains a sub-object whose contents depend on the breadcrumb type.
        /// Additional parameters that are unsupported by the type are rendered as a key/value table.
        /// </param>
        /// <param name="level">Breadcrumb level.</param>
        /// <seealso href="https://develop.sentry.dev/sdk/event-payloads/breadcrumbs/"/>
        [DebuggerStepThrough]
        public static void AddBreadcrumb(
            string message,
            string? category = null,
            string? type = null,
            IDictionary<string, string>? data = null,
            BreadcrumbLevel level = default)
            => CurrentHub.AddBreadcrumb(message, category, type, data, level);

        /// <summary>
        /// Adds a breadcrumb to the current scope.
        /// </summary>
        /// <remarks>
        /// This overload is intended to be used by integrations only.
        /// The objective is to allow better testability by allowing control of the timestamp set to the breadcrumb.
        /// </remarks>
        /// <param name="clock">An optional <see cref="ISystemClock"/>.</param>
        /// <param name="message">The message.</param>
        /// <param name="category">The category.</param>
        /// <param name="type">The type.</param>
        /// <param name="data">The data.</param>
        /// <param name="level">The level.</param>
        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddBreadcrumb(
            ISystemClock? clock,
            string message,
            string? category = null,
            string? type = null,
            IDictionary<string, string>? data = null,
            BreadcrumbLevel level = default)
            => CurrentHub.AddBreadcrumb(clock, message, category, type, data, level);

        /// <summary>
        /// Runs the callback with a new scope which gets dropped at the end.
        /// </summary>
        /// <remarks>
        /// Pushes a new scope, runs the callback, pops the scope.
        /// </remarks>
        /// <see href="https://docs.sentry.io/platforms/dotnet/enriching-events/scopes/#local-scopes"/>
        /// <param name="scopeCallback">The callback to run with the one time scope.</param>
        [Obsolete("This method is deprecated in favor of overloads of CaptureEvent, CaptureMessage and CaptureException " +
                  "that provide a callback to a configurable scope.")]
        [DebuggerStepThrough]
        public static void WithScope(Action<Scope> scopeCallback)
            => CurrentHub.WithScope(scopeCallback);

        /// <summary>
        /// Configures the scope through the callback.
        /// </summary>
        /// <param name="configureScope">The configure scope callback.</param>
        [DebuggerStepThrough]
        public static void ConfigureScope(Action<Scope> configureScope)
            => CurrentHub.ConfigureScope(configureScope);

        /// <summary>
        /// Configures the scope asynchronously.
        /// </summary>
        /// <param name="configureScope">The configure scope callback.</param>
        /// <returns>The Id of the event.</returns>
        [DebuggerStepThrough]
        public static Task ConfigureScopeAsync(Func<Scope, Task> configureScope)
            => CurrentHub.ConfigureScopeAsync(configureScope);

        /// <summary>
        /// Captures the event.
        /// </summary>
        /// <param name="evt">The event.</param>
        /// <returns>The Id of the event.</returns>
        [DebuggerStepThrough]
        public static SentryId CaptureEvent(SentryEvent evt)
            => CurrentHub.CaptureEvent(evt);

        /// <summary>
        /// Captures the event using the specified scope.
        /// </summary>
        /// <param name="evt">The event.</param>
        /// <param name="scope">The scope.</param>
        /// <returns>The Id of the event.</returns>
        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static SentryId CaptureEvent(SentryEvent evt, Scope? scope)
            => CurrentHub.CaptureEvent(evt, scope);

        /// <summary>
        /// Captures an event with a configurable scope.
        /// </summary>
        /// <remarks>
        /// This allows modifying a scope without affecting other events.
        /// </remarks>
        /// <param name="evt">The event.</param>
        /// <param name="configureScope">The callback to configure the scope.</param>
        /// <returns>The Id of the event.</returns>
        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static SentryId CaptureEvent(SentryEvent evt, Action<Scope> configureScope)
            => CurrentHub.CaptureEvent(evt, configureScope);

        /// <summary>
        /// Captures the exception.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>The Id of the even.t</returns>
        [DebuggerStepThrough]
        public static SentryId CaptureException(Exception exception)
            => CurrentHub.CaptureException(exception);

        /// <summary>
        /// Captures the exception with a configurable scope.
        /// </summary>
        /// <remarks>
        /// This allows modifying a scope without affecting other events.
        /// </remarks>
        /// <param name="exception">The exception.</param>
        /// <param name="configureScope">The callback to configure the scope.</param>
        /// <returns>The Id of the even.t</returns>
        [DebuggerStepThrough]
        public static SentryId CaptureException(Exception exception, Action<Scope> configureScope)
            => CurrentHub.CaptureException(exception, configureScope);

        /// <summary>
        /// Captures the message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="level">The message level.</param>
        /// <returns>The Id of the event.</returns>
        [DebuggerStepThrough]
        public static SentryId CaptureMessage(string message, SentryLevel level = SentryLevel.Info)
            => CurrentHub.CaptureMessage(message, level);

        /// <summary>
        /// Captures the message with a configurable scope.
        /// </summary>
        /// <remarks>
        /// This allows modifying a scope without affecting other events.
        /// </remarks>
        /// <param name="message">The message to send.</param>
        /// <param name="configureScope">The callback to configure the scope.</param>
        /// <param name="level">The message level.</param>
        /// <returns>The Id of the event.</returns>
        [DebuggerStepThrough]
        public static SentryId CaptureMessage(string message, Action<Scope> configureScope, SentryLevel level = SentryLevel.Info)
            => CurrentHub.CaptureMessage(message, configureScope, level);

        /// <summary>
        /// Captures a user feedback.
        /// </summary>
        /// <param name="userFeedback">The user feedback to send to Sentry.</param>
        [DebuggerStepThrough]
        public static void CaptureUserFeedback(UserFeedback userFeedback)
            => CurrentHub.CaptureUserFeedback(userFeedback);

        /// <summary>
        /// Captures a user feedback.
        /// </summary>
        /// <param name="eventId">The event Id.</param>
        /// <param name="email">The user email.</param>
        /// <param name="comments">The user comments.</param>
        /// <param name="name">The optional username.</param>
        [DebuggerStepThrough]
        public static void CaptureUserFeedback(SentryId eventId, string email, string comments, string? name = null)
            => CurrentHub.CaptureUserFeedback(new UserFeedback(eventId, name, email, comments));

        /// <summary>
        /// Captures a transaction.
        /// </summary>
        [DebuggerStepThrough]
        public static void CaptureTransaction(Transaction transaction)
            => CurrentHub.CaptureTransaction(transaction);

        /// <summary>
        /// Captures a session update.
        /// </summary>
        [DebuggerStepThrough]
        public static void CaptureSession(SessionUpdate sessionUpdate)
            => CurrentHub.CaptureSession(sessionUpdate);

        /// <summary>
        /// Starts a transaction.
        /// </summary>
        [DebuggerStepThrough]
        public static ITransaction StartTransaction(
            ITransactionContext context,
            IReadOnlyDictionary<string, object?> customSamplingContext)
            => CurrentHub.StartTransaction(context, customSamplingContext);

        /// <summary>
        /// Starts a transaction.
        /// </summary>
        [DebuggerStepThrough]
        internal static ITransaction StartTransaction(
            ITransactionContext context,
            IReadOnlyDictionary<string, object?> customSamplingContext,
            DynamicSamplingContext? dynamicSamplingContext)
            => CurrentHub.StartTransaction(context, customSamplingContext, dynamicSamplingContext);

        /// <summary>
        /// Starts a transaction.
        /// </summary>
        [DebuggerStepThrough]
        public static ITransaction StartTransaction(ITransactionContext context)
            => CurrentHub.StartTransaction(context);

        /// <summary>
        /// Starts a transaction.
        /// </summary>
        [DebuggerStepThrough]
        public static ITransaction StartTransaction(string name, string operation)
            => CurrentHub.StartTransaction(name, operation);

        /// <summary>
        /// Starts a transaction.
        /// </summary>
        [DebuggerStepThrough]
        public static ITransaction StartTransaction(string name, string operation, string? description)
            => CurrentHub.StartTransaction(name, operation, description);

        /// <summary>
        /// Starts a transaction.
        /// </summary>
        [DebuggerStepThrough]
        public static ITransaction StartTransaction(string name, string operation, SentryTraceHeader traceHeader)
            => CurrentHub.StartTransaction(name, operation, traceHeader);

        /// <summary>
        /// Binds specified exception the specified span.
        /// </summary>
        /// <remarks>
        /// This method is used internally and is not meant for public use.
        /// </remarks>
        [DebuggerStepThrough]
        public static void BindException(Exception exception, ISpan span)
            => CurrentHub.BindException(exception, span);

        /// <summary>
        /// Gets the last active span.
        /// </summary>
        [DebuggerStepThrough]
        public static ISpan? GetSpan()
            => CurrentHub.GetSpan();

        /// <summary>
        /// Gets the Sentry trace header.
        /// </summary>
        [DebuggerStepThrough]
        public static SentryTraceHeader? GetTraceHeader()
            => CurrentHub.GetTraceHeader();

        /// <inheritdoc cref="IHub.StartSession"/>
        [DebuggerStepThrough]
        public static void StartSession()
            => CurrentHub.StartSession();

        /// <inheritdoc cref="IHub.EndSession"/>
        [DebuggerStepThrough]
        public static void EndSession(SessionEndStatus status = SessionEndStatus.Exited)
            => CurrentHub.EndSession(status);

        /// <inheritdoc cref="IHub.PauseSession"/>
        [DebuggerStepThrough]
        public static void PauseSession()
            => CurrentHub.PauseSession();

        /// <inheritdoc cref="IHub.ResumeSession"/>
        [DebuggerStepThrough]
        public static void ResumeSession()
            => CurrentHub.ResumeSession();

        /// <summary>
        /// Deliberately crashes an application, which is useful for testing and demonstration purposes.
        /// </summary>
        /// <remarks>
        /// The method is marked obsolete only to discourage accidental misuse.
        /// We do not intend to remove it.
        /// </remarks>
        [Obsolete("WARNING: This method deliberately causes a crash, and should not be used in a real application.")]
        public static void CauseCrash(CrashType crashType)
        {
            var msg =
                "This exception was caused deliberately by " +
                $"{nameof(SentrySdk)}.{nameof(CauseCrash)}({nameof(CrashType)}.{crashType}).";

            switch (crashType)
            {
                case CrashType.Managed:
                    throw new ApplicationException(msg);

                case CrashType.ManagedBackgroundThread:
                    var thread = new Thread(() => throw new ApplicationException(msg));
                    thread.Start();
                    break;

#if ANDROID
                case CrashType.Java:
                    JavaSdk.Android.Supplemental.Buggy.ThrowRuntimeException(msg);
                    break;

                case CrashType.JavaBackgroundThread:
                    JavaSdk.Android.Supplemental.Buggy.ThrowRuntimeExceptionOnBackgroundThread(msg);
                    break;

                case CrashType.Native:
                    NativeCrash();
                    break;
#elif __IOS__
                case CrashType.Native:
                    SentryCocoaSdk.Crash();
                    break;
#endif
                default:
                    throw new ArgumentOutOfRangeException(nameof(crashType), crashType, null);
            }
        }

#if ANDROID
    [System.Runtime.InteropServices.DllImport("libsentrysupplemental.so", EntryPoint = "crash")]
    private static extern void NativeCrash();
#endif
    }
}

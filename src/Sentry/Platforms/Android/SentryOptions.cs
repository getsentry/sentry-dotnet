// ReSharper disable once CheckNamespace
namespace Sentry;

public partial class SentryOptions
{
    /// <summary>
    /// Exposes additional options for the Android platform.
    /// </summary>
    public AndroidOptions Android { get; } = new();

    /// <summary>
    /// Provides additional options for the Android platform.
    /// </summary>
    public class AndroidOptions
    {
        internal AndroidOptions() { }

        // ---------- From SentryAndroidOptions.java ----------

        /// <summary>
        /// Gets or sets a value that indicates if ANR (Application Not Responding) is enabled.
        /// The default value is <c>true</c> (enabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/android/configuration/app-not-respond/
        /// </remarks>
        public bool AnrEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value that indicates if ANR (Application Not Responding) is enabled on Debug mode.
        /// The default value is <c>false</c> (disabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/android/configuration/app-not-respond/
        /// </remarks>
        public bool AnrReportInDebug { get; set; }

        /// <summary>
        /// Gets or sets the ANR (Application Not Responding) timeout interval.
        /// The default values is 5 seconds.
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/android/configuration/app-not-respond/
        /// </remarks>
        public TimeSpan AnrTimeoutInterval { get; set; } = TimeSpan.FromSeconds(5);

        // TODO: Make this option work for .NET managed code
        /// <summary>
        /// Gets or sets a value that indicates whether to attach a screenshot when an error occurs.
        /// The default value is <c>false</c> (disabled).
        /// </summary>
        /// <remarks>
        /// This feature is provided by the Sentry Android SDK and thus only works for Java-based errors.
        /// See https://docs.sentry.io/platforms/android/enriching-events/screenshots/
        /// </remarks>
        public bool AttachScreenshot { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates if automatic breadcrumbs for <c>Activity</c> lifecycle events are
        /// enabled. The default value is <c>true</c> (enabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/android/enriching-events/breadcrumbs/
        /// </remarks>
        public bool EnableActivityLifecycleBreadcrumbs { get; set; } = true;

        /// <summary>
        /// Gets or sets a value that indicates if automatic breadcrumbs for <c>App</c> components are enabled.
        /// The default value is <c>true</c> (enabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/android/enriching-events/breadcrumbs/
        /// </remarks>
        public bool EnableAppComponentBreadcrumbs { get; set; } = true;

        /// <summary>
        /// Gets or sets a value that indicates if automatic breadcrumbs for <c>App</c> lifecycle events are enabled.
        /// The default value is <c>true</c> (enabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/android/enriching-events/breadcrumbs/
        /// </remarks>
        public bool EnableAppLifecycleBreadcrumbs { get; set; } = true;

        /// <summary>
        /// Gets or sets a value that indicates if automatic breadcrumbs for system events are enabled.
        /// The default value is <c>true</c> (enabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/android/enriching-events/breadcrumbs/
        /// </remarks>
        public bool EnableSystemEventBreadcrumbs { get; set; } = true;

        /// <summary>
        /// Gets or sets a value that indicates if automatic breadcrumbs for user interactions are enabled.
        /// The default value is <c>true</c> (enabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/android/enriching-events/breadcrumbs/
        /// </remarks>
        public bool EnableUserInteractionBreadcrumbs { get; set; } = true;

        /// <summary>
        /// Gets or sets a value that indicates if automatic instrumentation for <c>Activity</c> lifecycle tracing
        /// is enabled. The default value is <c>true</c> (enabled).
        /// Enabling this option also requires setting <see cref="SentryOptions.TracesSampleRate"/> or
        /// <see cref="SentryOptions.TracesSampler"/>. You can also control whether these transactions will
        /// finish automatically with <see cref="EnableActivityLifecycleTracingAutoFinish"/>.
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/android/performance/instrumentation/automatic-instrumentation/#androids-activity-instrumentation
        /// </remarks>
        public bool EnableAutoActivityLifecycleTracing { get; set; } = true;

        /// <summary>
        /// Gets or sets a value that indicates if automatic instrumentation for <c>Activity</c> lifecycle tracing
        /// should finish automatically. Requires <see cref="EnableAutoActivityLifecycleTracing"/> set <c>true</c>.
        /// The default value is <c>true</c> (enabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/android/performance/instrumentation/automatic-instrumentation/#androids-activity-instrumentation
        /// </remarks>
        public bool EnableActivityLifecycleTracingAutoFinish { get; set; } = true;

        /// <summary>
        /// Gets or sets a value that indicates if automatic instrumentation for user interaction tracing is enabled.
        /// The default value is <c>false</c> (disabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/android/performance/instrumentation/automatic-instrumentation/#user-interaction-instrumentation
        /// </remarks>
        public bool EnableUserInteractionTracing { get; set; }

        /// <summary>
        /// Gets or sets the interval for profiling traces, when enabled with <see cref="ProfilingEnabled"/>.
        /// The default value is 10 milliseconds.
        /// </summary>
        public TimeSpan ProfilingTracesInterval { get; set; } = TimeSpan.FromMilliseconds(10);


        // ---------- From SentryOptions.java ----------

        /// <summary>
        /// Gets or sets a value that indicates if all the threads are automatically attached to all logged events.
        /// The default value is <c>false</c> (disabled).
        /// </summary>
        public bool AttachThreads { get; set; }

        /// <summary>
        /// Gets or sets the connection timeout on the HTTP connection used by Java when sending data to Sentry.
        /// The default value is 5 seconds.
        /// </summary>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);

        // TODO: Should we have this Distribution property on SentryOptions (with Release and Environment)?
        /// <summary>
        /// Gets or sets the distribution.
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/java/guides/spring/configuration/#distribution
        /// </remarks>
        public string? Distribution { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates if the NDK (Android Native Development Kit) is enabled.
        /// The default value is <c>true</c> (enabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/android/using-ndk/#disable-ndk-integration
        /// </remarks>
        public bool EnableNdk { get; set; } = true;

        /// <summary>
        /// Gets or sets a value that indicates if the hook that flushes when the main Java thread shuts down
        /// is enabled. The default value is <c>true</c> (enabled).
        /// </summary>
        public bool EnableShutdownHook { get; set; } = true;

        /// <summary>
        /// Gets or sets a value that indicates if the handler that attaches to Java's
        /// <c>Thread.UncaughtExceptionHandler</c> is enabled. The default value is <c>true</c> (enabled).
        /// </summary>s
        public bool EnableUncaughtExceptionHandler { get; set; } = true;

        /// <summary>
        /// Gets or sets a value that indicates if uncaught Java errors will have their stack traces
        /// printed to the standard error stream. The default value is <c>false</c> (disabled).
        /// </summary>
        public bool PrintUncaughtStackTrace { get; set; }

        /// <summary>
        /// Gets or sets if profiling is enabled for transactions.
        /// The default value is <c>false</c> (disabled).
        /// See also <see cref="ProfilingTracesInterval"/>.
        /// </summary>
        public bool ProfilingEnabled { get; set; }

        /// <summary>
        /// Gets or sets the read timeout on the HTTP connection used by Java when sending data to Sentry.
        /// The default value is 5 seconds.
        /// </summary>
        public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(5);


        // ---------- Other ----------

        internal string[]? InAppExclude { get; set; }
        internal string[]? InAppInclude { get; set; }

        /// <summary>
        /// Add prefix to exclude from 'InApp' stacktrace list by the Android SDK.
        /// Note that this uses Java package names, not .NET namespaces.
        /// </summary>
        /// <param name="prefix">The string used to filter the stacktrace to be excluded from InApp.</param>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/java/configuration/options/#in-app-exclude
        /// </remarks>
        /// <example>
        /// 'java.util.', 'org.apache.logging.log4j.'
        /// </example>
        public void AddInAppExclude(string prefix) =>
            InAppExclude = InAppExclude != null
                ? InAppExclude.Concat(new[] { prefix }).ToArray()
                : new[] { prefix };

        /// <summary>
        /// Add prefix to include as in 'InApp' stacktrace by the Android SDK.
        /// Note that this uses Java package names, not .NET namespaces.
        /// </summary>
        /// <param name="prefix">The string used to filter the stacktrace to be included in InApp.</param>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/java/configuration/options/#in-app-include
        /// </remarks>
        /// <example>
        /// 'java.util.customcode.', 'io.sentry.samples.'
        /// </example>
        public void AddInAppInclude(string prefix) =>
            InAppInclude = InAppInclude != null
                ? InAppInclude.Concat(new[] { prefix }).ToArray()
                : new[] { prefix };

        /// <summary>
        /// Gets or sets a value that indicates if tracing features are enabled on the embedded Android SDK.
        /// The default value is <c>false</c> (disabled).
        /// </summary>
        public bool EnableAndroidSdkTracing { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates if the <see cref="BeforeSend"/> callback will be invoked for
        /// events that originate from the embedded Android SDK. The default value is <c>false</c> (disabled).
        /// </summary>
        /// <remarks>
        /// This is an experimental feature and is imperfect, as the .NET SDK and the embedded Android SDK don't
        /// implement all of the same features that may be present in the event graph. Some optional elements may
        /// be stripped away during the roundtripping between the two SDKs.  Use with caution.
        /// </remarks>
        public bool EnableAndroidSdkBeforeSend { get; set; }
    }
}

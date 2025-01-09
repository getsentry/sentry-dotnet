using ObjCRuntime;
using Sentry.Cocoa;
using Sentry.Extensibility;

// ReSharper disable once CheckNamespace
namespace Sentry;

public partial class SentryOptions
{
    /// <summary>
    /// Exposes additional options for iOS and MacCatalyst.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public NativeOptions Native { get; }

    /// <summary>
    /// Provides additional options for iOS and MacCatalyst.
    /// </summary>
    public class NativeOptions
    {
        private readonly SentryOptions _options;

        internal NativeOptions(SentryOptions options)
        {
            _options = options;
        }

        // ---------- From Cocoa's SentryOptions.h ----------

        /// <summary>
        /// Automatically attaches a screenshot when capturing an error or exception.
        /// The default value is <c>false</c> (disabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/apple/guides/ios/configuration/options/#attach-screenshot
        /// </remarks>
        public bool AttachScreenshot { get; set; } = false;

        /// <summary>
        /// The minimum amount of time an app should be unresponsive to be classified as an App Hanging.
        /// The actual amount may be a little longer.  Avoid using values lower than 100ms, which may cause a lot
        /// of app hangs events being transmitted.
        /// The default value is 2 seconds.
        /// Requires setting <see cref="EnableAppHangTracking"/> to <c>true</c>.
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/apple/configuration/app-hangs/
        /// </remarks>
        public TimeSpan AppHangTimeoutInterval { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// How long an idle transaction waits for new children after all its child spans finished.
        /// Only UI event transactions are idle transactions.
        /// The default value is 3 seconds.
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/apple/performance/instrumentation/automatic-instrumentation/#user-interaction-instrumentation
        /// </remarks>
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// When enabled, the SDK tracks when the application stops responding for a specific amount of
        /// time defined by the <see cref="AppHangTimeoutInterval"/> option.
        /// The default value is <c>true</c> (enabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/apple/configuration/app-hangs/
        /// </remarks>
        public bool EnableAppHangTracking { get; set; } = true;

        /// <summary>
        /// When enabled, the SDK adds breadcrumbs for various system events.
        /// The default value is <c>true</c> (enabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/apple/enriching-events/breadcrumbs/#automatic-breadcrumbs
        /// </remarks>
        public bool EnableAutoBreadcrumbTracking { get; set; } = true;

        /// <summary>
        /// When enabled, the SDK tracks performance for <see cref="UIViewController"/> subclasses and HTTP requests
        /// automatically. It also measures the app start and slow and frozen frames.
        /// The default value is <c>true</c> (enabled).
        /// </summary>
        /// <remarks>
        /// Performance Monitoring must be enabled for this option to take effect.
        /// See: https://docs.sentry.io/platforms/apple/performance/
        /// And: https://docs.sentry.io/platforms/apple/performance/instrumentation/automatic-instrumentation/#opt-out
        /// </remarks>
        public bool EnableAutoPerformanceTracing { get; set; } = true;

        /// <summary>
        /// When enabled, the SDK tracks the performance of Core Data operations.
        /// It requires enabling performance monitoring.
        /// The default value is <c>true</c> (enabled).
        /// </summary>
        /// <remarks>
        /// Performance Monitoring must be enabled for this option to take effect.
        /// See https://docs.sentry.io/platforms/apple/performance/instrumentation/automatic-instrumentation/#core-data-instrumentation
        /// </remarks>
        public bool EnableCoreDataTracing { get; set; } = true;

        /// <summary>
        /// When enabled, the SDK tracks performance for file IO reads and writes with <see cref="NSData"/>
        /// if auto performance tracking and <see cref="EnableSwizzling"/> are enabled.
        /// The default value is <c>true</c> (enabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/apple/performance/instrumentation/automatic-instrumentation/#file-io-instrumentation
        /// </remarks>
        public bool EnableFileIOTracing { get; set; } = true;

        /// <summary>
        /// When enabled, the SDK adds breadcrumbs for each network request
        /// if auto performance tracking and <see cref="EnableSwizzling"/> are enabled.
        /// The default value is <c>true</c> (enabled).
        /// </summary>
        public bool EnableNetworkBreadcrumbs { get; set; } = true;

        /// <summary>
        /// When enabled, the SDK adds breadcrumbs for HTTP requests and tracks performance for HTTP requests
        /// if auto performance tracking and <see cref="EnableSwizzling"/> are enabled.
        /// The default value is <c>true</c> (enabled).
        /// </summary>
        /// <remarks>
        /// https://docs.sentry.io/platforms/apple/performance/instrumentation/automatic-instrumentation/#http-instrumentation
        /// </remarks>
        public bool EnableNetworkTracking { get; set; } = true;

        /// <summary>
        /// Whether to enable watchdog termination tracking or not. NOT advised.
        /// The default value is <c>false</c> (disabled).
        /// </summary>
        /// <remarks>
        /// This feature is prone to false positives on .NET since it relies on heuristics that don't work in this environment.
        /// </remarks>
        /// <seealso href="https://github.com/getsentry/sentry-dotnet/issues/3860" />
        /// <seealso href="https://docs.sentry.io/platforms/apple/configuration/watchdog-terminations/" />
        [Obsolete("See: https://github.com/getsentry/sentry-dotnet/issues/3860")]
        public bool EnableWatchdogTerminationTracking { get; set; } = false;

        /// <summary>
        /// Whether the SDK should use swizzling or not.
        /// The default value is <c>true</c> (enabled).
        /// </summary>
        /// <remarks>
        /// When turned off the following features are disabled: breadcrumbs for touch events and
        /// navigation with <see cref="UIViewController"/>, automatic instrumentation for <see cref="UIViewController"/>,
        /// automatic instrumentation for HTTP requests, automatic instrumentation for file IO with <see cref="NSData"/>,
        /// and automatically added sentry-trace header to HTTP requests for distributed tracing.
        /// See https://docs.sentry.io/platforms/apple/configuration/swizzling/
        /// </remarks>
        public bool EnableSwizzling { get; set; } = true;

        /// <summary>
        /// When enabled, the SDK tracks performance for <see cref="UIViewController"/> subclasses.
        /// The default value is <c>true</c> (enabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/apple/performance/instrumentation/automatic-instrumentation/#uiviewcontroller-instrumentation
        /// </remarks>
        public bool EnableUIViewControllerTracing { get; set; } = true;

        /// <summary>
        /// When enabled, the SDK creates transactions for UI events like buttons clicks, switch toggles,
        /// and other UI elements that uses <see cref="UIControl.SendAction(Selector, NSObject?, UIEvent?)"/>.
        /// The default value is <c>false</c> (disabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/apple/performance/instrumentation/automatic-instrumentation/#user-interaction-instrumentation
        /// </remarks>
        public bool EnableUserInteractionTracing { get; set; } = false;

        /// <summary>
        /// When provided, this will be set as delegate on the <see cref="NSUrlSession"/> used for network
        /// data-transfer tasks performed by the native Sentry Cocoa SDK.
        /// </summary>
        /// <remarks>
        /// See https://github.com/getsentry/sentry-cocoa/issues/1168
        /// </remarks>
        public NSUrlSessionDelegate? UrlSessionDelegate { get; set; } = null;

        /// <summary>
        /// The SessionReplay Options from the Cocoa SDK.
        /// </summary>
        public NativeSentryReplayOptions SentryReplayOptions { get; set; } = new();


        // ---------- Other ----------

        /// <summary>
        /// Gets or sets a value that indicates if tracing features are enabled on the embedded Cocoa SDK.
        /// The default value is <c>false</c> (disabled).
        /// </summary>
        public bool EnableTracing { get; set; } = false;

        internal List<string>? InAppExcludes { get; private set; }
        internal List<string>? InAppIncludes { get; private set; }

        /// <summary>
        /// Add prefix to exclude from 'InApp' stacktrace list by the Cocoa SDK.
        /// Note that this uses iOS module names, not .NET namespaces.
        /// </summary>
        /// <param name="prefix">The string used to filter the stacktrace to be excluded from InApp.</param>
        /// <remarks>
        /// https://docs.sentry.io/platforms/apple/configuration/options/#in-app-exclude
        /// </remarks>
        public void AddInAppExclude(string prefix)
        {
            InAppExcludes ??= new List<string>();
            InAppExcludes.Add(prefix);
        }

        /// <summary>
        /// Add prefix to include as in 'InApp' stacktrace by the Cocoa SDK.
        /// Note that this uses iOS package names, not .NET namespaces.
        /// </summary>
        /// <param name="prefix">The string used to filter the stacktrace to be included in InApp.</param>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/apple/configuration/options/#in-app-include
        /// </remarks>
        public void AddInAppInclude(string prefix)
        {
            InAppIncludes ??= new List<string>();
            InAppIncludes.Add(prefix);
        }

        /// <summary>
        /// The SessionReplay Options from the Cocoa SDK.
        /// </summary>
        public class NativeSentryReplayOptions
        {
            /// <summary>
            /// Gets or sets the percentage of a session
            /// recording to be sent. Values should be between 0.0
            /// and 1.0. The default is 0.
            /// </summary>
            /// <remarks>
            /// <see href="https://docs.sentry.io/platforms/apple/guides/ios/session-replay/#sampling">sentry.io</see>
            /// </remarks>
            public float SessionSampleRate { get; set; }

            /// <summary>
            /// Gets or sets the percentage of an error
            /// recording to be sent. Values should be between 0.0
            /// and 1.0. The default is 0.
            /// </summary>
            /// <remarks>
            /// <see href="https://docs.sentry.io/platforms/apple/guides/ios/session-replay/#sampling">sentry.io</see>
            /// </remarks>
            public float OnErrorSampleRate { get; set; }

            /// <summary>
            /// Gets or sets a value determining whether
            /// text should be masked during recording.
            /// </summary>
            /// <remarks>
            /// <see href="https://docs.sentry.io/platforms/apple/guides/ios/session-replay/#privacy">sentry.io</see>
            /// </remarks>
            public bool MaskAllText { get; set; } = true;

            /// <summary>
            /// Gets or sets a value determining whether
            /// images should be masked during recording.
            /// </summary>
            /// <remarks>
            /// <see href="https://docs.sentry.io/platforms/apple/guides/ios/session-replay/#privacy">sentry.io</see>
            /// </remarks>
            public bool MaskAllImages { get; set; } = true;
        }
    }

    // We actually add the profiling integration automatically in InitSentryCocoaSdk().
    // However, if user calls AddProfilingIntegration() multiple times, we print a warning, as usual.
    private bool _profilingIntegrationAddedByUser = false;

    /// <summary>
    /// Adds ProfilingIntegration to Sentry.
    /// </summary>
    /// <param name="startupTimeout">
    /// Unused, only here so that the signature is the same as AddProfilingIntegration() from package Sentry.Profiling.
    /// </param>
    public void AddProfilingIntegration(TimeSpan startupTimeout = default)
    {
        if (HasIntegration<ProfilingIntegration>())
        {
            if (_profilingIntegrationAddedByUser)
            {
                DiagnosticLogger?.LogWarning($"{nameof(ProfilingIntegration)} has already been added. The second call to {nameof(AddProfilingIntegration)} will be ignored.");
            }
            return;
        }

        _profilingIntegrationAddedByUser = true;
        AddIntegration(new ProfilingIntegration());
    }

    /// <summary>
    /// Disables the Profiling integration.
    /// </summary>
    public void DisableProfilingIntegration()
    {
        _profilingIntegrationAddedByUser = false;
        RemoveIntegration<ProfilingIntegration>();
    }
}

using ObjCRuntime;

// ReSharper disable once CheckNamespace
namespace Sentry;

public partial class SentryOptions
{
    /// <summary>
    /// Exposes additional options for the iOS platform.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public IosOptions iOS { get; } = new();

    /// <summary>
    /// Provides additional options for the iOS platform.
    /// </summary>
    public class IosOptions
    {
        internal IosOptions() { }

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
        /// Gets or sets the distribution of the application.
        /// </summary>
        /// <remarks>
        /// See "dist" in https://develop.sentry.dev/sdk/event-payloads/#optional-attributes
        /// </remarks>
        // TODO: Should we have this property on the main SentryOptions (with Release and Environment)?
        public string? Distribution { get; set; } = null;

        /// <summary>
        /// When enabled, the SDK tracks when the application stops responding for a specific amount of
        /// time defined by the <see cref="AppHangTimeoutInterval"/> option.
        /// The default value is <c>false</c> (disabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/apple/configuration/app-hangs/
        /// </remarks>
        public bool EnableAppHangTracking { get; set; } = false;

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
        public bool EnableAutoPerformanceTracking { get; set; } = true;

        /// <summary>
        /// This feature is experimental.
        /// When enabled, the SDK tracks the performance of Core Data operations.
        /// It requires enabling performance monitoring.
        /// The default value is <c>false</c> (disabled).
        /// </summary>
        /// <remarks>
        /// Performance Monitoring must be enabled for this option to take effect.
        /// See https://docs.sentry.io/platforms/apple/performance/instrumentation/automatic-instrumentation/#core-data-instrumentation
        /// </remarks>
        public bool EnableCoreDataTracking { get; set; } = false;

        /// <summary>
        /// This feature is experimental.
        /// When enabled, the SDK tracks performance for file IO reads and writes with <see cref="NSData"/>
        /// if auto performance tracking and <see cref="EnableSwizzling"/> are enabled.
        /// The default value is <c>false</c> (disabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/apple/performance/instrumentation/automatic-instrumentation/#file-io-instrumentation
        /// </remarks>
        public bool EnableFileIOTracking { get; set; } = false;

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
        /// Whether to enable out of memory tracking or not.
        /// The default value is <c>true</c> (enabled).
        /// </summary>
        /// <remarks>
        /// https://docs.sentry.io/platforms/apple/configuration/out-of-memory/
        /// </remarks>
        public bool EnableOutOfMemoryTracking { get; set; } = true;

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
        public bool EnableUIViewControllerTracking { get; set; } = true;

        /// <summary>
        /// This feature is experimental.
        /// When enabled, the SDK creates transactions for UI events like buttons clicks, switch toggles,
        /// and other UI elements that uses <see cref="UIControl.SendAction(Selector, NSObject?, UIEvent?)"/>.
        /// The default value is <c>false</c> (disabled).
        /// </summary>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/apple/performance/instrumentation/automatic-instrumentation/#user-interaction-instrumentation
        /// </remarks>
        public bool EnableUserInteractionTracing { get; set; } = false;

        /// <summary>
        /// This feature is experimental.
        /// Turning this feature on can have an impact on the grouping of your issues.
        /// When enabled, the Cocoa SDK stitches stack traces of asynchronous code together.
        /// This feature affects stack traces from native crashes only.
        /// The default value is <c>false</c> (disabled).
        /// </summary>
        /// <remarks>
        /// Official docs TBD.
        /// See https://github.com/getsentry/sentry-docs/issues/5215
        /// </remarks>
        public bool StitchAsyncCode { get; set; } = false;

        // /// <summary>
        // /// This gets called shortly after the initialization of the SDK when the last program execution
        // /// terminated with a crash. It is not guaranteed that this is called on the main thread.
        // /// </summary>
        // /// <remarks>
        // /// This callback is only executed once during the entire run of the program to avoid
        // /// multiple callbacks if there are multiple crash events to send. This can happen when the program
        // /// terminates with a crash before the SDK can send the crash event.
        // /// You can use <see cref="BeforeSend"/> if you prefer a callback for every event.
        // /// See also https://docs.sentry.io/platforms/apple/enriching-events/user-feedback/
        // /// </remarks>
        // public Action<SentryEvent>? OnCrashedLastRun { get; set; } = null;

        /// <summary>
        /// When provided, this will be set as delegate on the <see cref="NSUrlSession"/> used for network
        /// data-transfer tasks performed by the native Sentry Cocoa SDK.
        /// </summary>
        /// <remarks>
        /// See https://github.com/getsentry/sentry-cocoa/issues/1168
        /// </remarks>
        public NSUrlSessionDelegate? UrlSessionDelegate { get; set; } = null;


        // ---------- Other ----------

        /// <summary>
        /// Gets or sets a value that indicates if tracing features are enabled on the embedded Cocoa SDK.
        /// The default value is <c>false</c> (disabled).
        /// </summary>
        public bool EnableCocoaSdkTracing { get; set; } = false;

        // /// <summary>
        // /// Gets or sets a value that indicates if the <see cref="BeforeSend"/> callback will be invoked for
        // /// events that originate from the embedded Cocoa SDK. The default value is <c>false</c> (disabled).
        // /// </summary>
        // /// <remarks>
        // /// This is an experimental feature and is imperfect, as the .NET SDK and the embedded Cocoa SDK don't
        // /// implement all of the same features that may be present in the event graph. Some optional elements may
        // /// be stripped away during the round-tripping between the two SDKs.  Use with caution.
        // /// </remarks>
        // public bool EnableCocoaSdkBeforeSend { get; set; }

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
    }
}

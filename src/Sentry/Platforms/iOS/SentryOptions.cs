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

        public bool AttachScreenshot { get; set; }
        public TimeSpan AppHangTimeoutInterval { get; set; }
        public TimeSpan IdleTimeout { get; set; }
        public string? Distribution { get; set; }
        public bool EnableAppHangTracking { get; set; }
        public bool EnableAutoBreadcrumbTracking { get; set; }
        public bool EnableAutoPerformanceTracking { get; set; }
        public bool EnableAutoSessionTracking { get; set; }
        public bool EnableCoreDataTracking { get; set; }
        public bool EnableFileIOTracking { get; set; }
        public bool EnableNetworkBreadcrumbs { get; set; }
        public bool EnableNetworkTracking { get; set; }
        public bool EnableOutOfMemoryTracking { get; set; }
        public bool EnableProfiling { get; set; }
        public bool EnableSwizzling { get; set; }
        public bool EnableUIViewControllerTracking { get; set; }
        public bool EnableUserInteractionTracing { get; set; }
        public bool StitchAsyncCode { get; set; }

        // public Action<SentryEvent>? OnCrashedLastRun { get; set; }

        public NSUrlSessionDelegate? UrlSessionDelegate { get; set; }


        // ---------- Other ----------

        /// <summary>
        /// Gets or sets a value that indicates if tracing features are enabled on the embedded Cocoa SDK.
        /// The default value is <c>false</c> (disabled).
        /// </summary>
        public bool EnableCocoaSdkTracing { get; set; }

        // /// <summary>
        // /// Gets or sets a value that indicates if the <see cref="BeforeSend"/> callback will be invoked for
        // /// events that originate from the embedded Cocoa SDK. The default value is <c>false</c> (disabled).
        // /// </summary>
        // /// <remarks>
        // /// This is an experimental feature and is imperfect, as the .NET SDK and the embedded Cocoa SDK don't
        // /// implement all of the same features that may be present in the event graph. Some optional elements may
        // /// be stripped away during the roundtripping between the two SDKs.  Use with caution.
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
        /// https://docs.sentry.io/platforms/apple/guides/ios/configuration/options/#in-app-exclude
        /// </remarks>
        public void AddInAppExclude(string prefix)
        {
            InAppExcludes ??= new List<string>();
            InAppExcludes.Add(prefix);
        }

        /// <summary>
        /// Add prefix to include as in 'InApp' stacktrace by the Cocoa SDK.
        /// Note that this uses Cocoa package names, not .NET namespaces.
        /// </summary>
        /// <param name="prefix">The string used to filter the stacktrace to be included in InApp.</param>
        /// <remarks>
        /// See https://docs.sentry.io/platforms/apple/guides/ios/configuration/options/#in-app-include
        /// </remarks>
        public void AddInAppInclude(string prefix)
        {
            InAppIncludes ??= new List<string>();
            InAppIncludes.Add(prefix);
        }
    }
}

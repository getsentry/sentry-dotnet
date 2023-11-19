// ReSharper disable once CheckNamespace
namespace Sentry;

internal partial class BindableSentryOptions
{
    public IosOptions iOS { get; } = new IosOptions();

    /// <summary>
    /// Provides additional options for the Android platform.
    /// </summary>
    public class IosOptions
    {
        public bool? AttachScreenshot { get; set; }
        public TimeSpan? AppHangTimeoutInterval { get; set; }
        public TimeSpan? IdleTimeout { get; set; }
        public bool? EnableAppHangTracking { get; set; }
        public bool? EnableAutoBreadcrumbTracking { get; set; }
        public bool? EnableAutoPerformanceTracing { get; set; }
        public bool? EnableCoreDataTracing { get; set; }
        public bool? EnableFileIOTracing { get; set; }
        public bool? EnableNetworkBreadcrumbs { get; set; }
        public bool? EnableNetworkTracking { get; set; }
        public bool? EnableWatchdogTerminationTracking { get; set; }
        public bool? EnableSwizzling { get; set; }
        public bool? EnableUIViewControllerTracing { get; set; }
        public bool? EnableUserInteractionTracing { get; set; }
        public bool? EnableCocoaSdkTracing { get; set; }

        public void ApplyTo(SentryOptions.IosOptions options)
        {
            options.AttachScreenshot = AttachScreenshot ?? options.AttachScreenshot;
            options.AppHangTimeoutInterval = AppHangTimeoutInterval ?? options.AppHangTimeoutInterval;
            options.IdleTimeout = IdleTimeout ?? options.IdleTimeout;
            options.EnableAppHangTracking = EnableAppHangTracking ?? options.EnableAppHangTracking;
            options.EnableAutoBreadcrumbTracking = EnableAutoBreadcrumbTracking ?? options.EnableAutoBreadcrumbTracking;
            options.EnableAutoPerformanceTracing = EnableAutoPerformanceTracing ?? options.EnableAutoPerformanceTracing;
            options.EnableCoreDataTracing = EnableCoreDataTracing ?? options.EnableCoreDataTracing;
            options.EnableFileIOTracing = EnableFileIOTracing ?? options.EnableFileIOTracing;
            options.EnableNetworkBreadcrumbs = EnableNetworkBreadcrumbs ?? options.EnableNetworkBreadcrumbs;
            options.EnableNetworkTracking = EnableNetworkTracking ?? options.EnableNetworkTracking;
            options.EnableWatchdogTerminationTracking = EnableWatchdogTerminationTracking ?? options.EnableWatchdogTerminationTracking;
            options.EnableSwizzling = EnableSwizzling ?? options.EnableSwizzling;
            options.EnableUIViewControllerTracing = EnableUIViewControllerTracing ?? options.EnableUIViewControllerTracing;
            options.EnableUserInteractionTracing = EnableUserInteractionTracing ?? options.EnableUserInteractionTracing;
            options.EnableCocoaSdkTracing = EnableCocoaSdkTracing ?? options.EnableCocoaSdkTracing;
        }
    }
}

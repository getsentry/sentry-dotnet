// ReSharper disable once CheckNamespace
namespace Sentry;

internal partial class BindableSentryOptions
{
    public NativeOptions Native { get; } = new NativeOptions();

    /// <summary>
    /// Provides additional options for the Android platform.
    /// </summary>
    public class NativeOptions
    {
        public bool? AttachScreenshot { get; set; }
        public TimeSpan? AppHangTimeoutInterval { get; set; }
        public TimeSpan? IdleTimeout { get; set; }
        public bool? EnableAppHangTracking { get; set; }
        public bool? EnableAppHangTrackingV2 { get; set; }
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
        public bool? EnableTracing { get; set; }
        public bool? SuppressSignalAborts { get; set; }
        public bool? SuppressExcBadAccess { get; set; }

        public void ApplyTo(SentryOptions.NativeOptions options)
        {
            options.AttachScreenshot = AttachScreenshot ?? options.AttachScreenshot;
            options.AppHangTimeoutInterval = AppHangTimeoutInterval ?? options.AppHangTimeoutInterval;
            options.IdleTimeout = IdleTimeout ?? options.IdleTimeout;
            options.EnableAppHangTracking = EnableAppHangTracking ?? options.EnableAppHangTracking;
            options.EnableAppHangTrackingV2 = EnableAppHangTrackingV2 ?? options.EnableAppHangTrackingV2;
            options.EnableAutoBreadcrumbTracking = EnableAutoBreadcrumbTracking ?? options.EnableAutoBreadcrumbTracking;
            options.EnableAutoPerformanceTracing = EnableAutoPerformanceTracing ?? options.EnableAutoPerformanceTracing;
            options.EnableCoreDataTracing = EnableCoreDataTracing ?? options.EnableCoreDataTracing;
            options.EnableFileIOTracing = EnableFileIOTracing ?? options.EnableFileIOTracing;
            options.EnableNetworkBreadcrumbs = EnableNetworkBreadcrumbs ?? options.EnableNetworkBreadcrumbs;
            options.EnableNetworkTracking = EnableNetworkTracking ?? options.EnableNetworkTracking;
#pragma warning disable CS0618 // Type or member is obsolete
            options.EnableWatchdogTerminationTracking = EnableWatchdogTerminationTracking ?? options.EnableWatchdogTerminationTracking;
#pragma warning restore CS0618 // Type or member is obsolete
            options.EnableSwizzling = EnableSwizzling ?? options.EnableSwizzling;
            options.EnableUIViewControllerTracing = EnableUIViewControllerTracing ?? options.EnableUIViewControllerTracing;
            options.EnableUserInteractionTracing = EnableUserInteractionTracing ?? options.EnableUserInteractionTracing;
            options.EnableTracing = EnableTracing ?? options.EnableTracing;
            options.SuppressSignalAborts = SuppressSignalAborts ?? options.SuppressSignalAborts;
            options.SuppressExcBadAccess = SuppressExcBadAccess ?? options.SuppressExcBadAccess;
        }
    }
}

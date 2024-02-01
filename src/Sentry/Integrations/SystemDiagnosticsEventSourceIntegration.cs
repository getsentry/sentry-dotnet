#if !__MOBILE__
using Sentry.Extensibility;
using Sentry.Internal;
#if NETFRAMEWORK
using Sentry.PlatformAbstractions;
#endif

namespace Sentry.Integrations;

internal class SystemDiagnosticsEventSourceIntegration : ISdkIntegration
{
    private readonly Action<ExperimentalMetricsOptions> _initializeListener;
    internal const string NoListenersAreConfiguredMessage = "SystemDiagnosticsEventSourceIntegration is disabled because no listeners are configured";
    internal const string MonoNotSupportedMessage = "SystemDiagnosticsEventSourceIntegration is disabled on Mono";

    public SystemDiagnosticsEventSourceIntegration()
    {
        _initializeListener = SystemDiagnosticsEventSourceListener.InitializeDefaultListener;
    }

    /// <summary>
    /// Overload for testing purposes
    /// </summary>
    internal SystemDiagnosticsEventSourceIntegration(Action<ExperimentalMetricsOptions> initializeListener)
    {
        _initializeListener = initializeListener;
    }

    public void Register(IHub hub, SentryOptions options)
    {
#if NETFRAMEWORK
        if (RuntimeInfo.GetRuntime().IsMono())
        {
            options.LogInfo(MonoNotSupportedMessage);
            return;
        }
#endif

        var captureEventSources = options.ExperimentalMetrics?.CaptureSystemDiagnosticsEventSources;
        if (captureEventSources is not { Count: > 0 })
        {
            options.LogInfo(NoListenersAreConfiguredMessage);
            return;
        }

        _initializeListener(options.ExperimentalMetrics!);
    }
}
#endif

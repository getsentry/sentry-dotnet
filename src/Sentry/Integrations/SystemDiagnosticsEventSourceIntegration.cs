using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Integrations;

internal class SystemDiagnosticsEventSourceIntegration : ISdkIntegration
{
    private readonly Action<ExperimentalMetricsOptions> _initializeListener;
    internal const string NoListenersAreConfiguredMessage = "SystemDiagnosticsEventSourceIntegration is disabled because no listeners are configured.";

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
        var captureEventSources = options.ExperimentalMetrics?.CaptureSystemDiagnosticsEventSourceNames;
        if (captureEventSources is not { Count: > 0 })
        {
            options.LogInfo(NoListenersAreConfiguredMessage);
            return;
        }

        _initializeListener(options.ExperimentalMetrics!);
    }
}

#if NET8_0_OR_GREATER
using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Integrations;

internal class SystemDiagnosticsMetricsIntegration : ISdkIntegration
{
    private readonly Action<ExperimentalMetricsOptions> _initializeListener;
    internal const string NoListenersAreConfiguredMessage = "System.Diagnostics.Metrics Integration is disabled because no listeners are configured.";

    public SystemDiagnosticsMetricsIntegration()
    {
        _initializeListener = SystemDiagnosticsMetricsListener.InitializeDefaultListener;
    }

    /// <summary>
    /// Overload for testing purposes
    /// </summary>
    internal SystemDiagnosticsMetricsIntegration(Action<ExperimentalMetricsOptions> initializeListener)
    {
        _initializeListener = initializeListener;
    }

    public void Register(IHub hub, SentryOptions options)
    {
        var captureInstruments = options.ExperimentalMetrics?.CaptureSystemDiagnosticsInstruments;
        var captureMeters = options.ExperimentalMetrics?.CaptureSystemDiagnosticsMeters;
        if (captureInstruments is not { Count: > 0 } && captureMeters is not { Count: > 0 })
        {
            options.LogInfo(NoListenersAreConfiguredMessage);
            return;
        }

        _initializeListener(options.ExperimentalMetrics!);
    }
}
#endif

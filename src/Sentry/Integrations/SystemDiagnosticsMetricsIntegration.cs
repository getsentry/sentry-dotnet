using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Integrations;

internal class SystemDiagnosticsMetricsIntegration : ISdkIntegration
{
    public void Register(IHub hub, SentryOptions options)
    {
        var captureInstruments = options.ExperimentalMetrics?.CaptureInstruments;
        if (captureInstruments is not { Count: > 0 })
        {
            options.LogInfo("System.Diagnostics.Metrics Integration is disabled because no listeners configured.");
            return;
        }

#if NET8_0_OR_GREATER
        SystemDiagnosticsMetricsListener.InitializeDefaultListener(captureInstruments);
#else
        options.LogInfo("System.Diagnostics.Metrics Integration is disabled because it requires .NET 8.0 or later.");
#endif
    }
}

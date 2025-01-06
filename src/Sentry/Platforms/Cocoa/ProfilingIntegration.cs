using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Cocoa;

/// <summary>
/// Enables transaction performance profiling.
/// </summary>
public class ProfilingIntegration : ISdkIntegration
{
    /// <inheritdoc/>
    public void Register(IHub hub, SentryOptions options)
    {
        if (options.IsProfilingEnabled)
        {
            try
            {
                options.LogDebug("Profiling is enabled, attaching native SDK profiler factory");
                options.TransactionProfilerFactory ??= new CocoaProfilerFactory(options);
            }
            catch (Exception e)
            {
                options.LogError(e, "Failed to initialize the profiler");
            }
        }
        else
        {
            options.LogInfo("Profiling Integration is disabled because profiling is disabled by configuration.");
        }
    }
}

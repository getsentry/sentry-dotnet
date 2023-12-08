using Sentry.Integrations;

namespace Sentry.Profiling;

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
            options.TransactionProfilerFactory = SamplingTransactionProfilerFactory.Create(options);
        }
    }
}

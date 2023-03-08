using Sentry.Integrations;

namespace Sentry.Extensions.Profiling;

/// <summary>
/// Enables transaction performance profiling.
/// </summary>
public class ProfilingIntegration : ISdkIntegration
{
    /// <inheritdoc/>
    public void Register(IHub hub, SentryOptions options)
    {
        options.TransactionProfilerFactory = new SamplingTransactionProfilerFactory();
    }
}

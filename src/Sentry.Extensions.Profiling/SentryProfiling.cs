using Sentry.Extensibility;
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
        if (string.IsNullOrEmpty(options.CacheDirectoryPath))
        {
            options.TransactionProfilerFactory = null;
            options.LogWarning($"Cannot use {typeof(ProfilingIntegration).Name} without CacheDirectoryPath.");
        }
        else
        {
            options.TransactionProfilerFactory = new SamplingTransactionProfilerFactory(options.CacheDirectoryPath);
        }
    }
}

using Sentry.Integrations;

namespace Sentry.Profiling;

/// <summary>
/// Enables transaction performance profiling.
/// </summary>
public class ProfilingIntegration : ISdkIntegration
{
    private string _tempDirectoryPath { get; set; }

    /// <summary>
    /// Initializes the the profiling integration.
    /// </summary>
    public ProfilingIntegration(string tempDirectoryPath)
    {
        _tempDirectoryPath = tempDirectoryPath;
    }

    /// <inheritdoc/>
    public void Register(IHub hub, SentryOptions options)
    {
        options.TransactionProfilerFactory = SamplingTransactionProfilerFactory.Create(options);
    }
}

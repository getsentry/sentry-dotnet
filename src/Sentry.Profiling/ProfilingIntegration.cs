using Sentry.Integrations;

namespace Sentry.Profiling;

/// <summary>
/// Enables transaction performance profiling.
/// </summary>
public class ProfilingIntegration : ISdkIntegration
{
    private TimeSpan _startupTimeout;

    /// <summary>
    /// Initializes the profiling integration.
    /// </summary>
    /// <param name="startupTimeout">
    /// If not given or TimeSpan.Zero, then the profiler initialization is asynchronous.
    /// This is useful for applications that need to start quickly. The profiler will start in the background
    /// and will be ready to capture transactions that have started after the profiler has started.
    ///
    /// If given a non-zero timeout, profiling startup blocks up to the given amount of time.
    /// </param>
    public ProfilingIntegration(TimeSpan startupTimeout = default)
    {
        _startupTimeout = startupTimeout;
    }

    /// <inheritdoc/>
    public void Register(IHub hub, SentryOptions options)
    {
        if (options.IsProfilingEnabled)
        {
            options.TransactionProfilerFactory ??= new SamplingTransactionProfilerFactory(options, _startupTimeout);
        }
    }
}

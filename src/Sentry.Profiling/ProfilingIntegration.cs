using Sentry.Extensibility;
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
    /// If given a non-zero timeout, profiling startup blocks up to the given amount of time. If the timeout is reached
    /// and the profiler session hasn't started yet, the execution is unblocked and behaves as the async startup,
    /// i.e. transactions will be profiled only after the session is eventually started.
    /// </param>
    public ProfilingIntegration(TimeSpan startupTimeout = default)
    {
        Debug.Assert(TimeSpan.Zero == default);
        _startupTimeout = startupTimeout;
    }

    /// <inheritdoc/>
    public void Register(IHub hub, SentryOptions options)
    {
        if (options.IsProfilingEnabled)
        {
            options.LogWarning("The profiling feature is currently experimental and may result in increased CPU and memory usage.");

            try
            {
                options.TransactionProfilerFactory ??= new SamplingTransactionProfilerFactory(options, _startupTimeout);
            }
            catch (Exception e)
            {
                options.LogError(e, "Failed to initialize the profiler");
            }
        }
    }
}

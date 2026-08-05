using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Profiling;

/// <summary>
/// Enables transaction performance profiling.
/// </summary>
public class ProfilingIntegration : ISdkIntegration, IDisposable
{
    private TimeSpan _startupTimeout;

    // Only set when this integration created the factory, so that Dispose() never tears down a
    // factory that was supplied by someone else.
    private IDisposable? _ownedFactory;

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
            try
            {
                if (options.TransactionProfilerFactory is null)
                {
                    var factory = new SamplingTransactionProfilerFactory(options, _startupTimeout);
                    options.TransactionProfilerFactory = factory;
                    _ownedFactory = factory;
                }
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

    /// <summary>
    /// Stops the profiler session started by this integration, releasing the underlying EventPipe
    /// session. Called by the SDK on shutdown.
    /// </summary>
    public void Dispose()
    {
        _ownedFactory?.Dispose();
        _ownedFactory = null;
    }
}

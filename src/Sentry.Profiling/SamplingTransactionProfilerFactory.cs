using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Profiling;

internal class SamplingTransactionProfilerFactory : ITransactionProfilerFactory
{
    // We only allow a single profile so let's keep track of the current status.
    internal int _inProgress = FALSE;

    private const int TRUE = 1;
    private const int FALSE = 0;

    // Stop profiling after the given number of milliseconds.
    private const int TIME_LIMIT_MS = 30_000;

    private readonly SentryOptions _options;

    public SamplingTransactionProfilerFactory(SentryOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public ITransactionProfiler? Start(ITransaction _, CancellationToken cancellationToken)
    {
        // Start a profiler if one wasn't running yet.
        if (Interlocked.Exchange(ref _inProgress, TRUE) == FALSE)
        {
            _options.LogDebug("Starting a sampling profiler session.");
            try
            {
                var profiler = new SamplingTransactionProfiler(_options, cancellationToken)
                {
                    OnFinish = () => _inProgress = FALSE
                };
                profiler.Start(TIME_LIMIT_MS);
                return profiler;
            }
            catch (Exception e)
            {
                _options.LogWarning("Failed to start a profiler session.", e);
                _inProgress = FALSE;
            }
        }
        return null;
    }
}

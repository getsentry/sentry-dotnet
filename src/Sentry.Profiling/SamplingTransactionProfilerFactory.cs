using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Profiling;

internal class SamplingTransactionProfilerFactory : IDisposable, ITransactionProfilerFactory
{
    // We only allow a single profile so let's keep track of the current status.
    internal int _inProgress = FALSE;

    private const int TRUE = 1;
    private const int FALSE = 0;

    // Stop profiling after the given number of milliseconds.
    private const int TIME_LIMIT_MS = 30_000;

    private readonly SentryOptions _options;
    private Task<SampleProfilerSession> _session;

    public static SamplingTransactionProfilerFactory Create(SentryOptions options)
    {
        var session = Task.Run(async () =>
        {
            var session = SampleProfilerSession.StartNew(options.DiagnosticLogger);
            await session.WaitForFirstEventAsync().ConfigureAwait(false);
            return session;
        });
        return new SamplingTransactionProfilerFactory(options, session);
    }

    private SamplingTransactionProfilerFactory(SentryOptions options, Task<SampleProfilerSession> session)
    {
        _options = options;
        _session = session;
    }

    /// <inheritdoc />
    public ITransactionProfiler? Start(ITransactionTracer _, CancellationToken cancellationToken)
    {
        // Start a profiler if one wasn't running yet.
        if (Interlocked.Exchange(ref _inProgress, TRUE) == FALSE)
        {
            if (!_session.IsCompletedSuccessfully)
            {
                _options.LogDebug("Cannot start a a sampling profiler, the session hasn't started yet.");
                _inProgress = FALSE;
                return null;
            }

            _options.LogDebug("Starting a sampling profiler.");
            try
            {
                return new SamplingTransactionProfiler(_options, _session.Result, TIME_LIMIT_MS, cancellationToken)
                {
                    OnFinish = () => _inProgress = FALSE
                };
            }
            catch (Exception e)
            {
                _options.LogError(e, "Failed to start a profiler session.");
                _inProgress = FALSE;
            }
        }
        return null;
    }

    public void Dispose()
    {
        _session.Dispose();
    }
}

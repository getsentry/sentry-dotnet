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

    internal Task<SampleProfilerSession> _sessionTask;

    private bool _errorLogged = false;

    public SamplingTransactionProfilerFactory(SentryOptions options, TimeSpan startupTimeout)
    {
        _options = options;

        _sessionTask = Task.Run(async () =>
        {
            // This can block up to 30 seconds. The timeout is out of our hands.
            var session = SampleProfilerSession.StartNew(options.DiagnosticLogger);

            // This can block indefinitely.
            await session.WaitForFirstEventAsync().ConfigureAwait(false);

            return session;
        });

        Debug.Assert(TimeSpan.FromSeconds(0) == TimeSpan.Zero);
        if (startupTimeout != TimeSpan.Zero && !_sessionTask.Wait(startupTimeout))
        {
            options.LogWarning("Profiler session startup took longer then the given timeout {0:c}. Profilling will start once the first event is received.", startupTimeout);
        }
    }

    /// <inheritdoc />
    public ITransactionProfiler? Start(ITransactionTracer _, CancellationToken cancellationToken)
    {
        // Start a profiler if one wasn't running yet.
        if (!_errorLogged && Interlocked.Exchange(ref _inProgress, TRUE) == FALSE)
        {
            if (!_sessionTask.IsCompleted)
            {
                _options.LogWarning("Cannot start a sampling profiler, the session hasn't started yet.");
                _inProgress = FALSE;
                return null;
            }

            if (!_sessionTask.IsCompletedSuccessfully)
            {
                _options.LogWarning("Cannot start a sampling profiler because the session startup has failed. This is a permanent error and no future transactions will be sampled.");
                _errorLogged = true;
                _inProgress = FALSE;
                return null;
            }

            _options.LogDebug("Starting a sampling profiler.");
            try
            {
                return new SamplingTransactionProfiler(_options, _sessionTask.Result, TIME_LIMIT_MS, cancellationToken)
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
        _sessionTask.ContinueWith((_session) => _session.Dispose());
    }
}

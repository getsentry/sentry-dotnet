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

    public SamplingTransactionProfilerFactory(SentryOptions options, TimeSpan startupTimeout)
    {
        _options = options;

        if (startupTimeout == TimeSpan.Zero)
        {
            _sessionTask = Task.Run(async () =>
            {
                var session = SampleProfilerSession.StartNew(options.DiagnosticLogger);
                await session.WaitForFirstEventAsync().ConfigureAwait(false);
                return session;
            });
        }
        else
        {
            var session = SampleProfilerSession.StartNew(options.DiagnosticLogger);
            var firstEventTask = session.WaitForFirstEventAsync();
            if (firstEventTask.Wait(startupTimeout))
            {
                _sessionTask = Task.FromResult(session);
            }
            else
            {
                options.LogWarning("Profiler session startup took longer then the given timeout {0:c}. Profilling will start once the first event is received.", startupTimeout);
                _sessionTask = firstEventTask.ContinueWith(_ => session);
            }
        }
    }

    /// <inheritdoc />
    public ITransactionProfiler? Start(ITransactionTracer _, CancellationToken cancellationToken)
    {
        // Start a profiler if one wasn't running yet.
        if (Interlocked.Exchange(ref _inProgress, TRUE) == FALSE)
        {
            if (!_sessionTask.IsCompletedSuccessfully)
            {
                _options.LogDebug("Cannot start a a sampling profiler, the session hasn't started yet.");
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

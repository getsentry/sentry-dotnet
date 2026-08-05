using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Profiling;

internal class SamplingTransactionProfilerFactory : IDisposable, ITransactionProfilerFactory
{
    // We only allow a single profile so let's keep track of the current status.
    internal InterlockedBoolean _inProgress = false;

    // Whether the session startup took longer than the given timeout.
    internal bool StartupTimedOut { get; }

    // Stop profiling after the given number of milliseconds.
    private const int TIME_LIMIT_MS = 30_000;

    // How long Dispose() waits for an in-flight session startup to complete before giving up on it.
    private const int SHUTDOWN_TIMEOUT_MS = 2_000;

    private readonly SentryOptions _options;

    internal Task<SampleProfilerSession> _sessionTask;

    // Cancels the wait for the first event so that Dispose() doesn't have to wait for a session that
    // may never receive one.
    private readonly CancellationTokenSource _shutdownCts = new();

    // Assigned as soon as the session exists, which is earlier than _sessionTask completing. Dispose()
    // uses this so it can also stop a session that never saw its first event.
    private SampleProfilerSession? _session;

    private int _disposed;

    // Exposed for tests.
    internal bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    private bool _errorLogged = false;

    public SamplingTransactionProfilerFactory(SentryOptions options, TimeSpan startupTimeout)
    {
        _options = options;

        _sessionTask = Task.Run(async () =>
        {
            // This can block up to 30 seconds. The timeout is out of our hands.
            var session = SampleProfilerSession.StartNew(options.DiagnosticLogger);
            _session = session;

            // This can block indefinitely, so it's cancelled when the factory is disposed.
            await session.WaitForFirstEventAsync(_shutdownCts.Token).ConfigureAwait(false);

            return session;
        });

        Debug.Assert(TimeSpan.FromSeconds(0) == TimeSpan.Zero);
        if (startupTimeout != TimeSpan.Zero && !_sessionTask.Wait(startupTimeout))
        {
            options.LogWarning("Profiler session startup took longer then the given timeout {0:c}. Profilling will start once the first event is received.", startupTimeout);
            StartupTimedOut = true;
        }
    }

    /// <inheritdoc />
    public ITransactionProfiler? Start(ITransactionTracer _, CancellationToken cancellationToken)
    {
        // Start a profiler if one wasn't running yet.
        if (!_errorLogged && !_inProgress.Exchange(true))
        {
            if (!_sessionTask.IsCompleted)
            {
                _options.LogWarning("Cannot start a sampling profiler, the session hasn't started yet.");
                _inProgress = false;
                return null;
            }

            if (!_sessionTask.IsCompletedSuccessfully)
            {
                _options.LogWarning("Cannot start a sampling profiler because the session startup has failed. This is a permanent error and no future transactions will be sampled.");
                _errorLogged = true;
                _inProgress = false;
                return null;
            }

            _options.LogDebug("Starting a sampling profiler.");
            try
            {
                return new SamplingTransactionProfiler(_options, _sessionTask.Result, TIME_LIMIT_MS, cancellationToken)
                {
                    OnFinish = () => _inProgress = false
                };
            }
            catch (Exception e)
            {
                _options.LogError(e, "Failed to start a profiler session.");
                _inProgress = false;
            }
        }
        return null;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        // Unblocks the startup task if it's still waiting for the first event to arrive.
        _shutdownCts.Cancel();

        try
        {
            // Gives an in-flight startup a chance to finish, and observes the exception if it failed
            // or was cancelled above.
            _sessionTask.Wait(SHUTDOWN_TIMEOUT_MS);
        }
        catch (Exception e)
        {
            _options.LogDebug("Profiler session didn't start up cleanly before shutdown: {0}", e.Message);
        }

        try
        {
            _session?.Dispose();
        }
        catch (Exception e)
        {
            _options.LogWarning(e, "Failed to stop the profiler session.");
        }

        _shutdownCts.Dispose();
    }
}

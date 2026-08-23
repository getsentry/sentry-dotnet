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

    // How long Dispose() waits for an in-flight session to complete before giving up on it.
    private const int SHUTDOWN_TIMEOUT_MS = 2_000;

    private readonly SentryOptions _options;

    internal Task<SampleProfilerSession> _sessionTask;

    private readonly CancellationTokenSource _shutdownCts = new();

    // StartNew() blocks uncancellably, so Dispose() may run before there is a session to stop.
    private readonly object _sessionLock = new();
    private SampleProfilerSession? _session;
    private bool _disposed;

    internal bool IsDisposed
    {
        get { lock (_sessionLock) { return _disposed; } }
    }

    private bool _errorLogged = false;

    public SamplingTransactionProfilerFactory(SentryOptions options, TimeSpan startupTimeout)
    {
        _options = options;

        // Store local reference to avoid ObjectDisposed exception
        var shutdownToken = _shutdownCts.Token;

        _sessionTask = Task.Run(async () =>
        {
            // This can block up to 30 seconds. The timeout is out of our hands.
            var session = SampleProfilerSession.StartNew(options.DiagnosticLogger);

            bool disposedWhileSessionStarting;
            lock (_sessionLock)
            {
                disposedWhileSessionStarting = _disposed;
                if (!disposedWhileSessionStarting)
                {
                    _session = session;
                }
            }

            if (disposedWhileSessionStarting)
            {
                session.Dispose();
                throw new OperationCanceledException(shutdownToken);
            }

            // This can block indefinitely.
            await session.WaitForFirstEventAsync(shutdownToken).ConfigureAwait(false);

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
        SampleProfilerSession? session;
        lock (_sessionLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // Null here means startup will see _disposed and stop its own session.
            session = _session;
        }

        _shutdownCts.Cancel();

        try
        {
            _sessionTask.Wait(SHUTDOWN_TIMEOUT_MS);
        }
        catch (Exception e)
        {
            _options.LogDebug("Profiler session didn't start up cleanly before shutdown: {0}", e.Message);
        }

        try
        {
            session?.Dispose();
        }
        catch (Exception e)
        {
            _options.LogWarning(e, "Failed to stop the profiler session.");
        }

        _shutdownCts.Dispose();
    }
}

using Microsoft.Diagnostics.Tracing;
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

    // Once the interning tables exceed this many entries we discard them ASAP
    internal static int MaxCallStackCount = 100_000;

    private readonly SentryOptions _options;

    internal Task<SampleProfilerSession> _sessionTask;

    private volatile SampleProfilerSession? _session;

    internal int TrimCount;

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

            _session = session;
            session.SampleEventParser.ThreadSample += TrimSessionStateIfNeeded;

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

    /// <summary>
    /// Discards TraceLog's call stack interning tables once they grow past <see cref="MaxCallStackCount"/>.
    /// <para>
    /// Runs for every sample, on the session's event processing thread - which is the only thread
    /// allowed to trim, so this is where it has to happen. It cannot be driven off profile completion
    /// alone, because the tables grow whether or not a profile is running.
    /// </para>
    /// <para>
    /// Safe to do mid-profile: SampleProfileBuilder keys a cache off CallStackIndex, which a trim
    /// reissues from zero, but it notices the bumped generation and drops that cache.
    /// </para>
    /// </summary>
    private void TrimSessionStateIfNeeded(TraceEvent data)
    {
        var session = _session;
        if (session is null || session.TraceLog.CallStacks.Count <= MaxCallStackCount)
        {
            return;
        }

        try
        {
            _options.LogDebug("Trimming profiler session state, {0} interned call stacks.", session.TraceLog.CallStacks.Count);
            // Costs the in-flight sample: its stack mapping goes with the tables, so AddSample skips it.
            session.TrimLiveSessionState();
            TrimCount++;
        }
        catch (Exception e)
        {
            // Never let this take down event processing for the whole session.
            _options.LogWarning(e, "Failed to trim profiler session state.");
        }
    }

    public void Dispose()
    {
        _sessionTask.ContinueWith(session => session.Dispose());
    }
}

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

    // TraceLog interns every distinct call stack it observes for the lifetime of the session and
    // never releases them, so a long-running process grows without bound. Once the interning tables
    // exceed this many entries we discard them at the next point where doing so is safe.
    // Exposed for tests.
    internal static int MaxCallStackCount = 100_000;

    private readonly SentryOptions _options;

    internal Task<SampleProfilerSession> _sessionTask;

    private volatile SampleProfilerSession? _session;

    // The end timestamp of the most recently finished profile. That profiler stays subscribed and
    // keeps consuming samples up to this timestamp, so we cannot trim until a later sample arrives.
    private double _lastProfileEndTimeMs = double.MinValue;

    // How many times we have trimmed. Exposed for tests.
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
                    OnFinish = endTimeMs =>
                    {
                        // Set the end time before clearing _inProgress, so the trim check can never
                        // observe "no profile running" together with a stale end time.
                        _lastProfileEndTimeMs = endTimeMs;
                        _inProgress = false;
                    }
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
    /// Runs for every sample, on the session's event processing thread - which is also the only thread
    /// allowed to call <c>TrimLiveSessionState</c>, so this is where the trim has to happen. It cannot be
    /// driven off profile completion alone, because the tables grow whether or not a profile is running.
    /// </para>
    /// </summary>
    private void TrimSessionStateIfNeeded(TraceEvent data)
    {
        // A profile is collecting. Its SampleProfileBuilder caches by CallStackIndex, and a trim
        // reissues those indexes from zero, so cached entries would resolve to unrelated stacks.
        if (_inProgress)
        {
            return;
        }

        // The previous profiler stays subscribed after finishing and keeps consuming samples up to its
        // end time. Only once we see a later sample do we know it has stopped resolving indexes.
        if (data.TimeStampRelativeMSec <= _lastProfileEndTimeMs)
        {
            return;
        }

        var traceLog = _session?.TraceLog;
        if (traceLog is null || traceLog.CallStacks.Count <= MaxCallStackCount)
        {
            return;
        }

        try
        {
            _options.LogDebug("Trimming profiler session state, {0} interned call stacks.", traceLog.CallStacks.Count);
            traceLog.TrimLiveSessionState();
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

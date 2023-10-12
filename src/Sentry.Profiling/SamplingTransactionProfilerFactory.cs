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
    private SampleProfilerSession _session;

    public static SamplingTransactionProfilerFactory Create(SentryOptions options)
    {
        var session = SampleProfilerSession.StartNew(options.DiagnosticLogger);
        return new SamplingTransactionProfilerFactory(options, session);
    }

    public static async Task<SamplingTransactionProfilerFactory> CreateAsync(SentryOptions options)
    {
        var session = await Task.Run(() => SampleProfilerSession.StartNew(options.DiagnosticLogger)).ConfigureAwait(false);
        return new SamplingTransactionProfilerFactory(options, session);
    }

    private SamplingTransactionProfilerFactory(SentryOptions options, SampleProfilerSession session)
    {
        _options = options;
        _session = session;
    }

    /// <inheritdoc />
    public ITransactionProfiler? Start(ITransaction _, CancellationToken cancellationToken)
    {
        // Start a profiler if one wasn't running yet.
        if (Interlocked.Exchange(ref _inProgress, TRUE) == FALSE)
        {
            _options.LogDebug("Starting a sampling profiler.");
            try
            {
                return new SamplingTransactionProfiler(_options, _session, TIME_LIMIT_MS, cancellationToken)
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

using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry.Profiling;

internal class SamplingTransactionProfiler : ITransactionProfiler
{
    public Action? OnFinish;
    private readonly CancellationToken _cancellationToken;
    private TimeSpan? _duration;
    private readonly SentryOptions _options;
    private TraceLogProcessor _processor;
    private SampleProfilerSession _session;

    public SamplingTransactionProfiler(SentryOptions options, SampleProfilerSession session, int timeoutMs, CancellationToken cancellationToken)
    {
        _options = options;
        _session = session;
        _cancellationToken = cancellationToken;
        _processor = new TraceLogProcessor(options, session.TraceLog, -session.Elapsed.TotalMilliseconds);
        session.SampleEventParser.ThreadSample += _processor.AddSample;
        cancellationToken.Register(() =>
        {
            if (Stop())
            {
                options.LogDebug("Profiling cancelled.");
            }
        });
        Task.Delay(timeoutMs, cancellationToken).ContinueWith(_ =>
        {
            if (Stop(TimeSpan.FromMilliseconds(timeoutMs)))
            {
                options.LogDebug("Profiling is being cut-of after {0} ms because the transaction takes longer than that.", timeoutMs);
            }
        }, CancellationToken.None);
    }

    private bool Stop(TimeSpan? duration = null)
    {
        if (_duration is null)
        {
            lock (_session)
            {
                if (_duration is null)
                {
                    _duration = duration ?? _session.Elapsed;
                    _session.SampleEventParser.ThreadSample -= _processor.AddSample;
                    return true;
                }
            }
        }
        return false;
    }

    /// <inheritdoc />
    public void Finish()
    {
        if (Stop())
        {
            _options.LogDebug("Profiling stopped on transaction finish.");
        }
    }

    // TODO doesn't need to be async anymore...
    /// <inheritdoc />
    public async Task<ProfileInfo> CollectAsync(Transaction transaction)
    {
        if (_duration is null)
        {
            throw new InvalidOperationException("Profiler.CollectAsync() called before Finish()");
        }
        _cancellationToken.ThrowIfCancellationRequested();
        return await Task.FromResult(CreateProfileInfo(transaction, _processor.Profile)).ConfigureAwait(false);
    }

    internal static ProfileInfo CreateProfileInfo(Transaction transaction, SampleProfile profile)
    {
        return new()
        {
            Contexts = transaction.Contexts,
            Environment = transaction.Environment,
            Transaction = transaction,
            // TODO FIXME - see https://github.com/getsentry/relay/pull/1902
            // Platform = transaction.Platform,
            Platform = "dotnet",
            Release = transaction.Release,
            StartTimestamp = transaction.StartTimestamp,
            Profile = profile
        };
    }
}

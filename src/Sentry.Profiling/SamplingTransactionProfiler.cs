using FastSerialization;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry.Profiling;

internal class SamplingTransactionProfiler : ITransactionProfiler
{
    public Action? OnFinish;
    private SampleProfilerSession? _session;
    private readonly CancellationToken _cancellationToken;
    private readonly SentryStopwatch _stopwatch = SentryStopwatch.StartNew();
    private TimeSpan? _duration;
    private Task<MemoryStream>? _data;
    private readonly string _tempDirectoryPath;
    private Transaction? _transaction;
    private readonly SentryOptions _options;

    public SamplingTransactionProfiler(string tempDirectoryPath, SentryOptions options, CancellationToken cancellationToken)
    {
        _options = options;
        _tempDirectoryPath = tempDirectoryPath;
        _cancellationToken = cancellationToken;
    }

    public void Start(int timeoutMs)
    {
        _session = SampleProfilerSession.StartNew(_cancellationToken);
        _cancellationToken.Register(() =>
        {
            if (Stop())
            {
                _options.LogDebug("Profiling cancelled.");
            }
        });
        Task.Delay(timeoutMs, _cancellationToken).ContinueWith(_ =>
        {
            if (Stop(TimeSpan.FromMilliseconds(timeoutMs)))
            {
                _options.LogDebug("Profiling is being cut-of after {0} ms because the transaction takes longer than that.", timeoutMs);
            }
        }, CancellationToken.None);
    }

    private bool Stop(TimeSpan? duration = null)
    {
        if (_duration is null && _session is not null)
        {
            lock (_session)
            {
                if (_duration is null)
                {
                    _duration = duration ?? _stopwatch.Elapsed;
                    try
                    {
                        // Stop the session synchronously so we can let the factory know it can start a new one.
                        _session.Stop();
                        OnFinish?.Invoke();
                        // Then finish collecting the data asynchronously.
                        _data = _session.FinishAsync();
                    }
                    catch (Exception e)
                    {
                        _options.LogWarning("Exception while stopping a profiler session.", e);
                    }
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

    /// <inheritdoc />
    public async Task<ProfileInfo> CollectAsync(Transaction transaction)
    {
        if (_data is null || _duration is null)
        {
            throw new InvalidOperationException("Profiler.CollectAsync() called before Finish()");
        }
        _options.LogDebug("Starting profile processing.");

        _transaction = transaction;
        using var nettraceStream = await _data.ConfigureAwait(false);
        using var eventPipeEventSource = new EventPipeEventSource(nettraceStream);
        using var traceLogEventSource = TraceLog.CreateFromEventPipeEventSource(eventPipeEventSource);

        _options.LogDebug("Converting profile to Sentry format.");

        _cancellationToken.ThrowIfCancellationRequested();
        var processor = new TraceLogProcessor(_options, traceLogEventSource)
        {
            MaxTimestampMs = _duration.Value.TotalMilliseconds
        };

        var profile = processor.Process(_cancellationToken);
        _options.LogDebug("Profiling finished successfully.");
        return CreateProfileInfo(transaction, profile);
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

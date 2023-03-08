using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry.Extensions.Profiling;

internal class SamplingTransactionProfilerFactory : ITransactionProfilerFactory
{
    // We only allow a single profile so let's keep track of the current status.
    internal int _inProgress = FALSE;

    const int TRUE = 1;
    const int FALSE = 0;

    // Stop profiling after the given number of milliseconds.
    const int TIME_LIMIT_MS = 30_000;

    /// <inheritdoc />
    public ITransactionProfiler? OnTransactionStart(ITransaction _, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Start a profiler if one wasn't running yet.
        if (Interlocked.Exchange(ref _inProgress, TRUE) == FALSE)
        {
            var profiler = new SamplingTransactionProfiler(now, cancellationToken, TIME_LIMIT_MS);
            profiler.OnFinish = () => _inProgress = FALSE;
            return profiler;
        }
        return null;
    }
}

internal class SamplingTransactionProfiler : ITransactionProfiler
{
    private SampleProfilerSession _session;
    private readonly CancellationToken _cancellationToken;
    private readonly DateTimeOffset _startTime;
    private DateTimeOffset? _endTime;
    private Task<MemoryStream>? _data;
    public Action? OnFinish;

    public SamplingTransactionProfiler(DateTimeOffset now, CancellationToken cancellationToken, int timeoutMs)
    {
        _startTime = now;
        _session = new(cancellationToken);
        _cancellationToken = cancellationToken;
        Task.Delay(timeoutMs, cancellationToken).ContinueWith(_ => Stop(now + TimeSpan.FromMilliseconds(timeoutMs)));
    }

    private void Stop(DateTimeOffset now)
    {
        if (_endTime is null)
        {
            lock (_session)
            {
                if (_endTime is null)
                {
                    _endTime = now;
                    _data = _session.Finish();
                }
            }
        }
    }

    /// <inheritdoc />
    public void OnTransactionFinish(DateTimeOffset now)
    {
        Stop(now);
        OnFinish?.Invoke();
    }

    /// <inheritdoc />
    public async Task<ProfileInfo?> Collect(Transaction transaction)
    {
        Debug.Assert(_data is not null, "OnTransactionFinish() wasn't called before Collect()");
        Debug.Assert(_endTime is not null);

        if (_data is null || _cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        var nettraceStream = await _data.ConfigureAwait(false);

        if (_cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        // TODO EventPipeEventSource(Stream stream) sets isStreaming = true even though the stream is pre-collected.
        //      This causes read issues when converting to ETLX. So we must write it to file first (or stream to file).
        // var eventSource = new EventPipeEventSource(nettraceStream);
        var etlFilePath = Path.GetTempFileName();
        using (FileStream file = new FileStream(etlFilePath, FileMode.Create, System.IO.FileAccess.Write))
        {
            nettraceStream.CopyTo(file);
            file.Flush();
            nettraceStream.Dispose();
        }
        var eventSource = new EventPipeEventSource(etlFilePath);

        if (_cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        var etlxFilePath = Path.GetTempFileName();
        try
        {
            // We convert the EventPipe log (ETL) to ETLX to get processed stack traces.
            // See https://github.com/microsoft/perfview/blob/main/documentation/TraceEvent/TraceEventProgrammersGuide.md#using-call-stacks-with-the-traceevent-library
            // NOTE: we may be able to skip collecting to the original ETL (nettrace) and just create ETLX directly, see CreateFromEventPipeDataFile() code.
            // ContinueOnError - best-effort if there's a broken trace. The resulting file may contain broken stacks as a result.
            etlxFilePath = TraceLog.CreateFromEventTraceLogFile(eventSource, etlxFilePath, new TraceLogOptions() { ContinueOnError = true });

            using var eventLog = new TraceLog(etlxFilePath);
            var processor = new TraceLogProcessor(eventLog);
            processor.MaxTimestampNs = (ulong)((_endTime.Value - _startTime).TotalMilliseconds * 1_000_000);
            var profile = processor.Process(_cancellationToken);
            if (_cancellationToken.IsCancellationRequested)
            {
                return null;
            }

            return new()
            {
                Contexts = transaction.Contexts,
                Environment = transaction.Environment,
                Transaction = transaction,
                // TODO FIXME - see https://github.com/getsentry/relay/pull/1902
                // Platform = transaction.Platform,
                Platform = "dotnet",
                Release = transaction.Release,
                StartTimestamp = _startTime,
                Profile = profile
            };
        }
        finally
        {
            if (File.Exists(etlxFilePath))
            {
                File.Delete(etlxFilePath);
            }
            if (File.Exists(etlFilePath))
            {
                // XXX
                // File.Move(etlFilePath, $"c:/dev/dotnet/temp/{transaction.EventId}.nettrace");
                File.Delete(etlFilePath);
            }
        }
    }
}

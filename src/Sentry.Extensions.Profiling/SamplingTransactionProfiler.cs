using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers;
using Sentry;
using Sentry.Extensions.Profiling;
using Sentry.Internal;
using Sentry.Protocol;

internal class ProfilerSession
{
    private DiagnosticsClient _client;
    private EventPipeSession _session;
    private MemoryStream _stream = new MemoryStream();
    private Task _copyTask;
    public readonly DateTimeOffset StartTimestamp;
    public readonly object? StartedBy;

    public ProfilerSession(object startedBy)
    {
        StartedBy = startedBy;
        _client = new DiagnosticsClient(Process.GetCurrentProcess().Id);

        var providers = new[]
        {
            new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, (long)ClrTraceEventParser.Keywords.Default),
            new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational, (long)ClrTraceEventParser.Keywords.None)
        };

        // The size of the runtime's buffer for collecting events in MB, same as the current default in StartEventPipeSession().
        var circularBufferMB = 256;
        StartTimestamp = DateTimeOffset.UtcNow;
        _session = _client.StartEventPipeSession(providers, true, circularBufferMB);
        _copyTask = _session.EventStream.CopyToAsync(_stream);
    }

    public MemoryStream Finish()
    {
        _session.Stop();
        _copyTask.Wait(1000); // TODO handle timeout
        return _stream;
    }
}

internal class SamplingTransactionProfiler : ITransactionProfiler
{
    private ProfilerSession? _session;

    public void OnTransactionStart(ITransaction transaction)
    {
        if (_session == null)
        {
            _session = new(transaction);
        }
    }

    public ProfileInfo? OnTransactionFinish(ITransaction transaction)
    {
        if (_session?.StartedBy == transaction)
        {
            var nettraceStream = _session.Finish();
            var startTimestamp = _session.StartTimestamp;
            _session = null;

            var eventSource = new EventPipeEventSource(nettraceStream);

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
                var profile = processor.Process();

                return new()
                {
                    Contexts = transaction.Contexts,
                    Environment = transaction.Environment,
                    Platform = transaction.Platform,
                    Release = transaction.Release,
                    StartTimestamp = startTimestamp,
                    Profile = profile
                };
            }
            finally
            {
                if (File.Exists(etlxFilePath))
                {
                    File.Delete(etlxFilePath);
                }
            }
        }

        return null;
    }
}

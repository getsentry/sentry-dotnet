using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace Sentry.Extensions.Profiling;

internal class SampleProfilerSession
{
    private DiagnosticsClient _client;
    private EventPipeSession _session;
    private MemoryStream _stream = new MemoryStream();
    private Task _copyTask;
    public readonly DateTimeOffset StartTimestamp;
    public readonly object? StartedBy;

    public SampleProfilerSession(object startedBy)
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
        _copyTask.Wait(10000); // TODO handle timeout
        _stream.Position = 0;
        return _stream;
    }
}

using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace Sentry.Profiling;

internal class SampleProfilerSession
{
    private readonly EventPipeSession _session;
    private readonly MemoryStream _stream;
    private readonly Task _copyTask;

    private readonly CancellationTokenRegistration _stopRegistration;

    private SampleProfilerSession(EventPipeSession session, MemoryStream stream, Task copyTask, CancellationTokenRegistration stopRegistration)
    {
        _session = session;
        _stream = stream;
        _copyTask = copyTask;
        _stopRegistration = stopRegistration;
    }

    public static SampleProfilerSession StartNew(CancellationToken cancellationToken)
    {
        var providers = new[]
        {
            // Note: all events we need issued by "DotNETRuntime" provider are at "EventLevel.Informational"
            // see https://learn.microsoft.com/en-us/dotnet/fundamentals/diagnostics/runtime-events
            new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, (long)ClrTraceEventParser.Keywords.Default),
            new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational),
            new EventPipeProvider("System.Threading.Tasks.TplEventSource", EventLevel.Informational, (long)TplEtwProviderTraceEventParser.Keywords.Default)
        };


        // The size of the runtime's buffer for collecting events in MB, same as the current default in StartEventPipeSession().
        var circularBufferMB = 256;

        var client = new DiagnosticsClient(Process.GetCurrentProcess().Id);
        var session = client.StartEventPipeSession(providers, true, circularBufferMB);
        var stopRegistration = cancellationToken.Register(() => session.Stop(), false);
        var stream = new MemoryStream();
        var copyTask = session.EventStream.CopyToAsync(stream, cancellationToken);

        return new SampleProfilerSession(session, stream, copyTask, stopRegistration);
    }

    public async Task<MemoryStream> FinishAsync()
    {
        _stopRegistration.Unregister();
        await _session.StopAsync(CancellationToken.None).ConfigureAwait(false);
        await _copyTask.ConfigureAwait(false);
        _stream.Position = 0;
        return _stream;
    }
}

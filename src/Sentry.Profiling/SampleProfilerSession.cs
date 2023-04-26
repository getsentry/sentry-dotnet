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
    private bool _stopped;

    private SampleProfilerSession(EventPipeSession session, MemoryStream stream, Task copyTask, CancellationTokenRegistration stopRegistration)
    {
        _session = session;
        _stream = stream;
        _copyTask = copyTask;
        _stopRegistration = stopRegistration;
    }

    // Exposed only for benchmarks.
    internal static EventPipeProvider[] Providers = new[]
    {
        // Note: all events we need issued by "DotNETRuntime" provider are at "EventLevel.Informational"
        // see https://learn.microsoft.com/en-us/dotnet/fundamentals/diagnostics/runtime-events
        new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, (long) ClrTraceEventParser.Keywords.Default),
        new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational),
        new EventPipeProvider("System.Threading.Tasks.TplEventSource", EventLevel.Informational, (long) TplEtwProviderTraceEventParser.Keywords.Default)
    };

    // Exposed only for benchmarks.
    internal static bool RequestRundown = true;

    // Exposed only for benchmarks.
    // The size of the runtime's buffer for collecting events in MB, same as the current default in StartEventPipeSession().
    internal static int CircularBufferMB = 256;

    public static SampleProfilerSession StartNew(CancellationToken cancellationToken)
    {
        var client = new DiagnosticsClient(Process.GetCurrentProcess().Id);
        var session = client.StartEventPipeSession(Providers, RequestRundown, CircularBufferMB);
        var stopRegistration = cancellationToken.Register(() => session.Stop(), false);
        var stream = new MemoryStream();
        var copyTask = session.EventStream.CopyToAsync(stream, cancellationToken);

        return new SampleProfilerSession(session, stream, copyTask, stopRegistration);
    }

    public void Stop()
    {
        if (!_stopped)
        {
            _stopRegistration.Unregister();
            _session.Stop();
            _stopped = true;
        }
    }

    public async Task<MemoryStream> FinishAsync()
    {
        Stop();
        await _copyTask.ConfigureAwait(false);
        _session.Dispose();
        _stream.Position = 0;
        return _stream;
    }
}

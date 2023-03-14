using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing.Parsers;

namespace Sentry.Profiling;

internal class SampleProfilerSession
{
    private DiagnosticsClient _client;
    private EventPipeSession _session;
    private MemoryStream _stream = new MemoryStream();
    private Task _copyTask;
    private CancellationToken _cancellationToken;
    private CancellationTokenRegistration _cancellationTokenStopRegistration;

    public SampleProfilerSession(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _client = new DiagnosticsClient(Process.GetCurrentProcess().Id);

        var providers = new[]
        {
            // Note: all events we need issued by "DotNETRuntime" provider are at "EventLevel.Informational"
            // see https://learn.microsoft.com/en-us/dotnet/fundamentals/diagnostics/runtime-events
            new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Informational, (long)ClrTraceEventParser.Keywords.Default),
            new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Informational, (long)ClrTraceEventParser.Keywords.None)
        };

        // The size of the runtime's buffer for collecting events in MB, same as the current default in StartEventPipeSession().
        var circularBufferMB = 256;
        _session = _client.StartEventPipeSession(providers, true, circularBufferMB);
        _copyTask = _session.EventStream.CopyToAsync(_stream, _cancellationToken);
        _cancellationTokenStopRegistration = _cancellationToken.Register(() => _session.Stop(), false);
    }

    public async Task<MemoryStream> Finish()
    {
        _cancellationTokenStopRegistration.Unregister();
        await Task.WhenAny(_session.StopAsync(CancellationToken.None), Task.Delay(10_000)).ConfigureAwait(false);
        await _copyTask.ConfigureAwait(false);
        _stream.Position = 0;
        return _stream;
    }
}

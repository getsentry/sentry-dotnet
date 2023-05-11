using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.EventPipe;
using Microsoft.Diagnostics.Tracing.Parsers;
using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Profiling;

internal class SampleProfilerSession : IDisposable
{
    private readonly EventPipeSession _session;
    private readonly TraceLogEventSource _eventSource;
    private readonly SampleProfilerTraceEventParser _sampleEventParser;
    private readonly IDiagnosticLogger? _logger;
    private readonly SentryStopwatch _stopwatch = SentryStopwatch.StartNew();
    private bool _stopped = false;

    private SampleProfilerSession(EventPipeSession session, TraceLogEventSource eventSource, IDiagnosticLogger? logger)
    {
        _session = session;
        _logger = logger;
        _eventSource = eventSource;
        _sampleEventParser = new SampleProfilerTraceEventParser(_eventSource);
    }

    // Exposed only for benchmarks.
    internal static EventPipeProvider[] Providers = new[]
    {
        // Note: all events we need issued by "DotNETRuntime" provider are at "EventLevel.Informational"
        // see https://learn.microsoft.com/en-us/dotnet/fundamentals/diagnostics/runtime-events
        // TODO replace Keywords.Default with a subset. Currently it is:
        //   Default = GC | Type | GCHeapSurvivalAndMovement | Binder | Loader | Jit | NGen | SupressNGen
        //                | StopEnumeration | Security | AppDomainResourceManagement | Exception | Threading | Contention | Stack | JittedMethodILToNativeMap
        //                | ThreadTransfer | GCHeapAndTypeNames | Codesymbols | Compilation,
        new EventPipeProvider(ClrTraceEventParser.ProviderName, EventLevel.Informational, (long) ClrTraceEventParser.Keywords.Default),
        new EventPipeProvider(SampleProfilerTraceEventParser.ProviderName, EventLevel.Informational),
        // new EventPipeProvider(TplEtwProviderTraceEventParser.ProviderName, EventLevel.Informational, (long) TplEtwProviderTraceEventParser.Keywords.Default)
    };

    // Exposed only for benchmarks.
    // The size of the runtime's buffer for collecting events in MB, same as the current default in StartEventPipeSession().
    internal static int CircularBufferMB = 256;

    public SampleProfilerTraceEventParser SampleEventParser => _sampleEventParser;

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public TraceLog TraceLog => _eventSource.TraceLog;

    public static SampleProfilerSession StartNew(IDiagnosticLogger? logger = null)
    {
        var client = new DiagnosticsClient(Process.GetCurrentProcess().Id);

        // Rundown events only come in after the session is stopped but we need them right from the start so that
        // TraceLog has all the info about loaded moodules and methods at the time a sample is handled.
        // As is, we can just disable the rundown and rely on the currently loaded modules.
        var session = client.StartEventPipeSession(Providers, false, CircularBufferMB);

        var eventSource = TraceLog.CreateFromEventPipeSession(session);

        // Process() blocks until the session is stopped so we need to run it on a separate thread.
        Task.Factory.StartNew(eventSource.Process, TaskCreationOptions.LongRunning);

        return new SampleProfilerSession(session, eventSource, logger);
    }

    public void Stop()
    {
        if (!_stopped)
        {
            try
            {
                _stopped = true;
                _session.Stop();
                _session.Dispose();
                _eventSource.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Error during sampler profiler session shutdown.", ex);
            }
        }
    }

    public void Dispose() => Stop();
}

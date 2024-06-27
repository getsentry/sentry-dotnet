using System.Diagnostics.Tracing;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Diagnostics.Tracing;
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
    private readonly SentryStopwatch _stopwatch;
    private bool _stopped = false;

    private SampleProfilerSession(SentryStopwatch stopwatch, EventPipeSession session, TraceLogEventSource eventSource, IDiagnosticLogger? logger)
    {
        _session = session;
        _logger = logger;
        _eventSource = eventSource;
        _sampleEventParser = new SampleProfilerTraceEventParser(_eventSource);
        _stopwatch = stopwatch;
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
    // The size of the runtime's buffer for collecting events. The docs are sparse but it seems like we don't
    // need a large buffer if we're connecting righ away. Leaving it too large increases app memory usage.
    internal static int CircularBufferMB = 16;

    public SampleProfilerTraceEventParser SampleEventParser => _sampleEventParser;

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public TraceLog TraceLog => _eventSource.TraceLog;

    // default is false, set 1 for true.
    private static int _throwOnNextStartupForTests = 0;

    internal static bool ThrowOnNextStartupForTests
    {
        get { return Interlocked.CompareExchange(ref _throwOnNextStartupForTests, 1, 1) == 1; }
        set
        {
            if (value)
                Interlocked.CompareExchange(ref _throwOnNextStartupForTests, 1, 0);
            else
                Interlocked.CompareExchange(ref _throwOnNextStartupForTests, 0, 1);
        }
    }

    public static SampleProfilerSession StartNew(IDiagnosticLogger? logger = null)
    {
        try
        {
            var client = new DiagnosticsClient(Environment.ProcessId);

            if (Interlocked.CompareExchange(ref _throwOnNextStartupForTests, 0, 1) == 1)
            {
                throw new Exception("Test exception");
            }

            // Note: StartEventPipeSession() can time out after 30 seconds on resource constrained systems.
            // See https://github.com/dotnet/diagnostics/blob/991c78895323a953008e15fe34b736c03706afda/src/Microsoft.Diagnostics.NETCore.Client/DiagnosticsIpc/IpcClient.cs#L40C52-L40C52
            var session = client.StartEventPipeSession(Providers, requestRundown: false, CircularBufferMB);

            var stopWatch = SentryStopwatch.StartNew();
            var eventSource = TraceLog.CreateFromEventPipeSession(session);

            // Process() blocks until the session is stopped so we need to run it on a separate thread.
            Task.Factory.StartNew(eventSource.Process, TaskCreationOptions.LongRunning)
                .ContinueWith(_ =>
                {
                    if (_.Exception?.InnerException is { } e)
                    {
                        logger?.LogWarning(e, "Error during sampler profiler EventPipeSession processing.");
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);

            return new SampleProfilerSession(stopWatch, session, eventSource, logger);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Error during sampler profiler EventPipeSession startup.");
            throw;
        }
    }

    public async Task WaitForFirstEventAsync(CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource();
        var cb = (TraceEvent _) => { tcs.TrySetResult(); };
        _eventSource.AllEvents += cb;
        try
        {
            // Wait for the first event to be processed.
            await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _eventSource.AllEvents -= cb;
        }
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
                _logger?.LogWarning(ex, "Error during sampler profiler session shutdown.");
            }
        }
    }

    public void Dispose() => Stop();
}

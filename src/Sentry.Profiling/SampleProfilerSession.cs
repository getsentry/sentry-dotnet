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
    private readonly SampleProfilerTraceEventParser _sampleEventParser;
    private readonly IDiagnosticLogger? _logger;
    private readonly SentryStopwatch _stopwatch;
    private bool _stopped = false;
    private Task _processing;

    private SampleProfilerSession(SentryStopwatch stopwatch, EventPipeSession session, TraceLogEventSource eventSource, Task processing, IDiagnosticLogger? logger)
    {
        _session = session;
        _logger = logger;
        EventSource = eventSource;
        _sampleEventParser = new SampleProfilerTraceEventParser(EventSource);
        _stopwatch = stopwatch;
        _processing = processing;
    }

    // Exposed only for benchmarks.
    internal static EventPipeProvider[] Providers = new[]
    {
        new EventPipeProvider(ClrTraceEventParser.ProviderName, EventLevel.Verbose, (long) (
            ClrTraceEventParser.Keywords.Jit
            | ClrTraceEventParser.Keywords.NGen
            | ClrTraceEventParser.Keywords.Loader
            | ClrTraceEventParser.Keywords.Binder
            | ClrTraceEventParser.Keywords.JittedMethodILToNativeMap
            )),
        new EventPipeProvider(SampleProfilerTraceEventParser.ProviderName, EventLevel.Informational),
        // new EventPipeProvider(TplEtwProviderTraceEventParser.ProviderName, EventLevel.Informational, (long) TplEtwProviderTraceEventParser.Keywords.Default)
    };

    // Exposed only for benchmarks.
    // The size of the runtime's buffer for collecting events. The docs are sparse but it seems like we don't
    // need a large buffer if we're connecting righ away. Leaving it too large increases app memory usage.
    internal static int CircularBufferMB = 16;

    // How long Stop() waits for the event processing task to drain after the session has been stopped.
    // Draining should be near-instant; this only bounds the worst case so shutdown can't hang.
    internal const int ProcessingDrainTimeoutMs = 2_000;

    // Exposed for tests
    internal TraceLogEventSource EventSource { get; }

    public SampleProfilerTraceEventParser SampleEventParser => _sampleEventParser;

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public TraceLog TraceLog => EventSource.TraceLog;

    private static InterlockedBoolean _throwOnNextStartupForTests = false;

    internal static bool ThrowOnNextStartupForTests
    {
        get { return _throwOnNextStartupForTests; }
        set { _throwOnNextStartupForTests.Exchange(value); }
    }

    public static SampleProfilerSession StartNew(IDiagnosticLogger? logger = null)
    {
        try
        {
            var client = new DiagnosticsClient(Environment.ProcessId);

            if (_throwOnNextStartupForTests.CompareExchange(false, true) == true)
            {
                throw new Exception("Test exception");
            }

            // Note: StartEventPipeSession() can time out after 30 seconds on resource constrained systems.
            // See https://github.com/dotnet/diagnostics/blob/991c78895323a953008e15fe34b736c03706afda/src/Microsoft.Diagnostics.NETCore.Client/DiagnosticsIpc/IpcClient.cs#L40C52-L40C52
            var session = client.StartEventPipeSession(Providers, requestRundown: false, CircularBufferMB);

            var stopWatch = SentryStopwatch.StartNew();
            var eventSource = TraceLog.CreateFromEventPipeSession(session, TraceLog.EventPipeRundownConfiguration.Enable(client));

            // Process() blocks until the session is stopped so we need to run it on a separate thread.
            // Note: the continuation is deliberately unconditional. A continuation whose criteria aren't
            // met (e.g. OnlyOnFaulted when Process() returns normally) transitions to Canceled, which
            // would make the Wait() in Stop() throw on every clean shutdown.
            var processing = Task.Factory.StartNew(eventSource.Process, TaskCreationOptions.LongRunning)
                .ContinueWith(_ =>
                {
                    if (_.Exception?.InnerException is { } e)
                    {
                        logger?.LogWarning(e, "Error during sampler profiler EventPipeSession processing.");
                    }
                });

            return new SampleProfilerSession(stopWatch, session, eventSource, processing, logger);
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
        EventSource.AllEvents += cb;
        try
        {
            // Wait for the first event to be processed.
            await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            EventSource.AllEvents -= cb;
        }
    }

    public void Stop()
    {
        if (_stopped)
        {
            return;
        }

        _stopped = true;
        try
        {
            _session.Stop();

            // Let the processing task drain the events that are still in flight, but don't hold up
            // shutdown indefinitely if it doesn't get there.
            if (!_processing.Wait(ProcessingDrainTimeoutMs))
            {
                _logger?.LogWarning("Sampler profiler event processing didn't finish within {0} ms of stopping the session.", ProcessingDrainTimeoutMs);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error during sampler profiler session shutdown.");
        }
        finally
        {
            // These need to happen even if stopping the session or draining the events failed, otherwise
            // the EventPipe connection to the runtime is left open.
            try
            {
                _session.Dispose();
                EventSource.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error disposing the sampler profiler session.");
            }
        }
    }

    public void Dispose() => Stop();
}

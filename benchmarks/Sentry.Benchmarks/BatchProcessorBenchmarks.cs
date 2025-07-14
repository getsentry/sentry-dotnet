using BenchmarkDotNet.Attributes;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Testing;

namespace Sentry.Benchmarks;

public class BatchProcessorBenchmarks
{
    private BatchProcessor _batchProcessor;
    private SentryLog _log;

    [Params(10, 100)]
    public int BatchCount { get; set; }

    [Params(100, 200, 1_000)]
    public int OperationsPerInvoke { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var hub = DisabledHub.Instance;
        var batchInterval = Timeout.InfiniteTimeSpan;
        var clock = new MockClock();
        var clientReportRecorder = Substitute.For<IClientReportRecorder>();
        var diagnosticLogger = Substitute.For<IDiagnosticLogger>();
        _batchProcessor = new BatchProcessor(hub, BatchCount, batchInterval, clock, clientReportRecorder, diagnosticLogger);

        _log = new SentryLog(DateTimeOffset.Now, SentryId.Empty, SentryLogLevel.Trace, "message");
    }

    [Benchmark]
    public void EnqueueAndFlush()
    {
        for (var i = 0; i < OperationsPerInvoke; i++)
        {
            _batchProcessor.Enqueue(_log);
        }
        _batchProcessor.Flush();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _batchProcessor.Dispose();
    }
}

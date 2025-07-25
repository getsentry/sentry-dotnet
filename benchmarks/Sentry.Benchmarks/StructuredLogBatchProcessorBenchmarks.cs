using BenchmarkDotNet.Attributes;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Benchmarks;

public class StructuredLogBatchProcessorBenchmarks
{
    private StructuredLogBatchProcessor _batchProcessor;
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
        var clientReportRecorder = Substitute.For<IClientReportRecorder>();
        var diagnosticLogger = Substitute.For<IDiagnosticLogger>();
        _batchProcessor = new StructuredLogBatchProcessor(hub, BatchCount, batchInterval, clientReportRecorder, diagnosticLogger);

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

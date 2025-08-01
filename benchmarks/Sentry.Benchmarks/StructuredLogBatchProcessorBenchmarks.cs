using BenchmarkDotNet.Attributes;
using NSubstitute;
using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Benchmarks;

public class StructuredLogBatchProcessorBenchmarks
{
    private Hub _hub;
    private StructuredLogBatchProcessor _batchProcessor;
    private SentryLog _log;

    [Params(10, 100)]
    public int BatchCount { get; set; }

    [Params(100, 200, 1_000)]
    public int OperationsPerInvoke { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        SentryOptions options = new()
        {
            Dsn = DsnSamples.ValidDsn,
            Experimental =
            {
                EnableLogs = true,
            },
        };

        var batchInterval = Timeout.InfiniteTimeSpan;

        var clientReportRecorder = Substitute.For<IClientReportRecorder>();
        clientReportRecorder
            .When(static recorder => recorder.RecordDiscardedEvent(Arg.Any<DiscardReason>(), Arg.Any<DataCategory>(), Arg.Any<int>()))
            .Throw<UnreachableException>();

        var diagnosticLogger = Substitute.For<IDiagnosticLogger>();
        diagnosticLogger
            .When(static logger => logger.IsEnabled(Arg.Any<SentryLevel>()))
            .Throw<UnreachableException>();
        diagnosticLogger
            .When(static logger => logger.Log(Arg.Any<SentryLevel>(), Arg.Any<string>(), Arg.Any<Exception>(), Arg.Any<object[]>()))
            .Throw<UnreachableException>();

        _hub = new Hub(options, DisabledHub.Instance);
        _batchProcessor = new StructuredLogBatchProcessor(_hub, BatchCount, batchInterval, clientReportRecorder, diagnosticLogger);
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
        _hub.Dispose();
    }
}

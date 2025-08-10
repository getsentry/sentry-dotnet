using BenchmarkDotNet.Attributes;
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
        var clientReportRecorder = new NullClientReportRecorder();

        _hub = new Hub(options, DisabledHub.Instance);
        _batchProcessor = new StructuredLogBatchProcessor(_hub, BatchCount, batchInterval, clientReportRecorder, null);
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

file sealed class NullClientReportRecorder : IClientReportRecorder
{
    public void RecordDiscardedEvent(DiscardReason reason, DataCategory category, int quantity = 1)
    {
        // no-op
    }

    public ClientReport GenerateClientReport()
    {
        throw new UnreachableException();
    }

    public void Load(ClientReport report)
    {
        throw new UnreachableException();
    }
}

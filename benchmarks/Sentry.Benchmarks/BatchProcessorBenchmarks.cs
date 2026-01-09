using BenchmarkDotNet.Attributes;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry.Benchmarks;

/// <summary>
/// <see cref="BatchProcessor{TItem}"/> (formerly "Sentry.Internal.StructuredLogBatchProcessor") was originally developed as Batch Processor for Logs only.
/// When adding support for Trace-connected Metrics, which are quite similar to Logs, it has been made generic to support both.
/// For comparability of results, we still benchmark with <see cref="SentryLog"/>, rather than <see cref="SentryMetric"/>.
/// </summary>
public class BatchProcessorBenchmarks
{
    private Hub _hub;
    private BatchProcessor<SentryLog> _batchProcessor;
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
            EnableLogs = true,
        };

        var batchInterval = Timeout.InfiniteTimeSpan;
        var clientReportRecorder = new NullClientReportRecorder();

        _hub = new Hub(options, DisabledHub.Instance);
        _batchProcessor = new BatchProcessor<SentryLog>(_hub, BatchCount, batchInterval, StructuredLog.Capture, clientReportRecorder, null);
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

    [Benchmark]
    public void EnqueueAndFlush_Parallel()
    {
        _ = Parallel.For(0, OperationsPerInvoke, (int i) =>
        {
            _batchProcessor.Enqueue(_log);
        });
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

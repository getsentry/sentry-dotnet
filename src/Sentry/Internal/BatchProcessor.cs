using System.Timers;
using Sentry.Protocol;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal;

/// <summary>
/// The Sentry Batch Processor.
/// This implementation is not complete yet.
/// Also, the specification is still work in progress.
/// </summary>
/// <remarks>
/// Sentry Specification: <see href="https://develop.sentry.dev/sdk/telemetry/spans/batch-processor/"/>.
/// OpenTelemetry spec: <see href="https://github.com/open-telemetry/opentelemetry-collector/blob/main/processor/batchprocessor/README.md"/>.
/// </remarks>
internal sealed class BatchProcessor : IDisposable
{
    private readonly IHub _hub;
    private readonly BatchProcessorTimer _timer;
    private readonly BatchBuffer<SentryLog> _logs;
    private readonly object _lock;

    private DateTime _lastFlush = DateTime.MinValue;

    public BatchProcessor(IHub hub, int batchCount, TimeSpan batchInterval)
        : this(hub, batchCount, new TimersBatchProcessorTimer(batchInterval))
    {
    }

    public BatchProcessor(IHub hub, int batchCount, BatchProcessorTimer timer)
    {
        _hub = hub;

        _timer = timer;
        _timer.Elapsed += OnIntervalElapsed;

        _logs = new BatchBuffer<SentryLog>(batchCount);
        _lock = new object();
    }

    internal void Enqueue(SentryLog log)
    {
        lock (_lock)
        {
            EnqueueCore(log);
        }
    }

    private void EnqueueCore(SentryLog log)
    {
        var isFirstLog = _logs.IsEmpty;
        var added = _logs.TryAdd(log);
        Debug.Assert(added, $"Since we currently have no lock-free scenario, it's unexpected to exceed the {nameof(BatchBuffer<SentryLog>)}'s capacity.");

        if (isFirstLog && !_logs.IsFull)
        {
            _timer.Enabled = true;
        }
        else if (_logs.IsFull)
        {
            _timer.Enabled = false;
            Flush();
        }
    }

    private void Flush()
    {
        _lastFlush = DateTime.UtcNow;

        var logs = _logs.ToArrayAndClear();
        _ = _hub.CaptureEnvelope(Envelope.FromLog(new StructuredLog(logs)));
    }

    private void OnIntervalElapsed(object? sender, ElapsedEventArgs e)
    {
        _timer.Enabled = false;

        lock (_lock)
        {
            if (!_logs.IsEmpty && e.SignalTime > _lastFlush)
            {
                Flush();
            }
        }
    }

    public void Dispose()
    {
        _timer.Enabled = false;
        _timer.Elapsed -= OnIntervalElapsed;
        _timer.Dispose();
    }
}

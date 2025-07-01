using Sentry.Extensibility;
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
    private readonly TimeSpan _batchInterval;
    private readonly IDiagnosticLogger? _diagnosticLogger;
    private readonly Timer _timer;
    private readonly BatchBuffer<SentryLog> _logs;
    private readonly object _lock;

    private DateTime _lastFlush = DateTime.MinValue;

    public BatchProcessor(IHub hub, int batchCount, TimeSpan batchInterval, IDiagnosticLogger? diagnosticLogger)
    {
        _hub = hub;
        _batchInterval = batchInterval;
        _diagnosticLogger = diagnosticLogger;

        _timer = new Timer(OnIntervalElapsed, this, Timeout.Infinite, Timeout.Infinite);

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
            EnableTimer();
        }
        else if (_logs.IsFull)
        {
            DisableTimer();
            Flush();
        }
    }

    private void Flush()
    {
        _lastFlush = DateTime.UtcNow;

        var logs = _logs.ToArrayAndClear();
        _ = _hub.CaptureEnvelope(Envelope.FromLog(new StructuredLog(logs)));
    }

    internal void OnIntervalElapsed(object? state)
    {
        lock (_lock)
        {
            if (!_logs.IsEmpty && DateTime.UtcNow > _lastFlush)
            {
                Flush();
            }
        }
    }

    private void EnableTimer()
    {
        var updated = _timer.Change(_batchInterval, Timeout.InfiniteTimeSpan);
        Debug.Assert(updated, "Timer was not successfully enabled.");
    }

    private void DisableTimer()
    {
        var updated = _timer.Change(Timeout.Infinite, Timeout.Infinite);
        Debug.Assert(updated, "Timer was not successfully disabled.");
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}

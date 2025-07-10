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
    private readonly IClientReportRecorder _clientReportRecorder;
    private readonly IDiagnosticLogger? _diagnosticLogger;

    private readonly Timer _timer;
    private readonly object _timerCallbackLock;
    private readonly BatchBuffer<SentryLog> _buffer1;
    private readonly BatchBuffer<SentryLog> _buffer2;
    private volatile BatchBuffer<SentryLog> _activeBuffer;

    private DateTime _lastFlush = DateTime.MinValue;

    public BatchProcessor(IHub hub, int batchCount, TimeSpan batchInterval, IClientReportRecorder clientReportRecorder, IDiagnosticLogger? diagnosticLogger)
    {
        _hub = hub;
        _batchInterval = batchInterval;
        _clientReportRecorder = clientReportRecorder;
        _diagnosticLogger = diagnosticLogger;

        _timer = new Timer(OnIntervalElapsed, this, Timeout.Infinite, Timeout.Infinite);
        _timerCallbackLock = new object();

        _buffer1 = new BatchBuffer<SentryLog>(batchCount);
        _buffer2 = new BatchBuffer<SentryLog>(batchCount);
        _activeBuffer = _buffer1;
    }

    internal void Enqueue(SentryLog log)
    {
        var activeBuffer = _activeBuffer;

        if (!TryEnqueue(activeBuffer, log))
        {
            activeBuffer = activeBuffer == _buffer1 ? _buffer2 : _buffer1;
            if (!TryEnqueue(activeBuffer, log))
            {
                _clientReportRecorder.RecordDiscardedEvent(DiscardReason.Backpressure, DataCategory.Default, 1);
                _diagnosticLogger?.LogInfo("Log Buffer full ... dropping log");
            }
        }
    }

    private bool TryEnqueue(BatchBuffer<SentryLog> buffer, SentryLog log)
    {
        if (buffer.TryAdd(log, out var count))
        {
            if (count == 1) // is first element added to buffer after flushed
            {
                EnableTimer();
            }

            if (count == buffer.Capacity) // is buffer full
            {
                // swap active buffer atomically
                var currentActiveBuffer = _activeBuffer;
                var newActiveBuffer = ReferenceEquals(currentActiveBuffer, _buffer1) ? _buffer2 : _buffer1;
                if (Interlocked.CompareExchange(ref _activeBuffer, newActiveBuffer, currentActiveBuffer) == currentActiveBuffer)
                {
                    DisableTimer();
                    Flush(buffer, count);
                }
            }

            return true;
        }

        return false;
    }

    private void Flush(BatchBuffer<SentryLog> buffer)
    {
        _lastFlush = DateTime.UtcNow;

        var logs = buffer.ToArrayAndClear();
        _ = _hub.CaptureEnvelope(Envelope.FromLog(new StructuredLog(logs)));
    }

    private void Flush(BatchBuffer<SentryLog> buffer, int count)
    {
        _lastFlush = DateTime.UtcNow;

        var logs = buffer.ToArrayAndClear(count);
        _ = _hub.CaptureEnvelope(Envelope.FromLog(new StructuredLog(logs)));
    }

    internal void OnIntervalElapsed(object? state)
    {
        lock (_timerCallbackLock)
        {
            var currentActiveBuffer = _activeBuffer;

            if (!currentActiveBuffer.IsEmpty && DateTime.UtcNow > _lastFlush)
            {
                var newActiveBuffer = ReferenceEquals(currentActiveBuffer, _buffer1) ? _buffer2 : _buffer1;
                if (Interlocked.CompareExchange(ref _activeBuffer, newActiveBuffer, currentActiveBuffer) == currentActiveBuffer)
                {
                    Flush(currentActiveBuffer);
                }
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

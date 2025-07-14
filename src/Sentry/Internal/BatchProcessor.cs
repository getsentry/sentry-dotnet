using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Protocol;
using Sentry.Protocol.Envelopes;
using Sentry.Threading;

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
    private readonly ISystemClock _clock;
    private readonly IClientReportRecorder _clientReportRecorder;
    private readonly IDiagnosticLogger? _diagnosticLogger;

    private readonly Timer _timer;
    private readonly object _timerCallbackLock;
    private readonly BatchBuffer<SentryLog> _buffer1;
    private readonly BatchBuffer<SentryLog> _buffer2;
    private volatile BatchBuffer<SentryLog> _activeBuffer;
    private readonly NonReentrantLock _swapLock;

    private DateTimeOffset _lastFlush = DateTimeOffset.MinValue;

    public BatchProcessor(IHub hub, int batchCount, TimeSpan batchInterval, ISystemClock clock, IClientReportRecorder clientReportRecorder, IDiagnosticLogger? diagnosticLogger)
    {
        _hub = hub;
        _batchInterval = batchInterval;
        _clock = clock;
        _clientReportRecorder = clientReportRecorder;
        _diagnosticLogger = diagnosticLogger;

        _timer = new Timer(OnIntervalElapsed, this, Timeout.Infinite, Timeout.Infinite);
        _timerCallbackLock = new object();

        _buffer1 = new BatchBuffer<SentryLog>(batchCount, "Buffer 1");
        _buffer2 = new BatchBuffer<SentryLog>(batchCount, "Buffer 2");
        _activeBuffer = _buffer1;
        _swapLock = new NonReentrantLock();
    }

    internal void Enqueue(SentryLog log)
    {
        var activeBuffer = _activeBuffer;

        if (!TryEnqueue(activeBuffer, log))
        {
            activeBuffer = ReferenceEquals(activeBuffer, _buffer1) ? _buffer2 : _buffer1;
            if (!TryEnqueue(activeBuffer, log))
            {
                _clientReportRecorder.RecordDiscardedEvent(DiscardReason.Backpressure, DataCategory.Default, 1);
                _diagnosticLogger?.LogInfo("Log Buffer full ... dropping log");
            }
        }
    }

    internal void Flush()
    {
        DisableTimer();
        Flush(_buffer1);
        Flush(_buffer2);
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
                using var flushScope = buffer.TryEnterFlushScope(out var lockTaken);
                DisableTimer();

                var currentActiveBuffer = _activeBuffer;
                _ = TrySwapBuffer(currentActiveBuffer);
                Flush(buffer, count);
            }

            return true;
        }

        return false;
    }

    private void Flush(BatchBuffer<SentryLog> buffer)
    {
        buffer.WaitAddCompleted();
        _lastFlush = _clock.GetUtcNow();

        var logs = buffer.ToArrayAndClear();
        _ = _hub.CaptureEnvelope(Envelope.FromLog(new StructuredLog(logs)));
    }

    private void Flush(BatchBuffer<SentryLog> buffer, int count)
    {
        buffer.WaitAddCompleted();
        _lastFlush = _clock.GetUtcNow();

        var logs = buffer.ToArrayAndClear(count);
        _ = _hub.CaptureEnvelope(Envelope.FromLog(new StructuredLog(logs)));
    }

    internal void OnIntervalElapsed(object? state)
    {
        lock (_timerCallbackLock)
        {
            var currentActiveBuffer = _activeBuffer;

            using var scope = currentActiveBuffer.TryEnterFlushScope(out var lockTaken);

            if (lockTaken && !currentActiveBuffer.IsEmpty && _clock.GetUtcNow() > _lastFlush)
            {
                _ = TrySwapBuffer(currentActiveBuffer);
                Flush(currentActiveBuffer);
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

    private bool TrySwapBuffer(BatchBuffer<SentryLog> currentActiveBuffer)
    {
        if (_swapLock.TryEnter())
        {
            var newActiveBuffer = ReferenceEquals(currentActiveBuffer, _buffer1) ? _buffer2 : _buffer1;
            var previousActiveBuffer = Interlocked.CompareExchange(ref _activeBuffer, newActiveBuffer, currentActiveBuffer);

            _swapLock.Exit();
            return previousActiveBuffer == currentActiveBuffer;
        }

        return false;
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}

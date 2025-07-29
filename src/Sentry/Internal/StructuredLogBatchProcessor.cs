using Sentry.Extensibility;
using Sentry.Protocol;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal;

/// <summary>
/// The Batch Processor for Sentry Logs.
/// </summary>
/// <remarks>
/// Uses a double buffer strategy to achieve synchronous and lock-free adding.
/// Switches the active buffer either when full or timeout exceeded (after first item added).
/// Logs are dropped when both buffers are either full or being flushed.
/// Logs are not enqueued when the Hub is disabled (Hub is being or has been disposed).
/// Flushing blocks the calling thread until all pending add operations have completed.
/// <code>
/// Implementation:
/// - When Hub is disabled (i.e. disposed), does not enqueue log
/// - Try to enqueue log into currently active buffer
///   - when currently active buffer is full, try to enqueue log into the other buffer
///   - when the other buffer is also full, or currently being flushed, then the log is dropped and a discarded event is recorded as a client report
/// - Swap currently active buffer when
///   - buffer is full
///   - timeout has exceeded
/// - Batch and Capture logs after swapping currently active buffer
///   - wait until all pending add/enqueue operations have completed (required for timeout)
///   - flush the buffer and capture an envelope containing the batched logs
/// - After flush, logs can be enqueued again into the buffer
/// </code>
/// </remarks>
/// <seealso href="https://develop.sentry.dev/sdk/telemetry/logs/">Sentry Logs</seealso>
/// <seealso href="https://develop.sentry.dev/sdk/telemetry/spans/batch-processor/">Sentry Batch Processor</seealso>
/// <seealso href="https://github.com/open-telemetry/opentelemetry-collector/blob/main/processor/batchprocessor/README.md">OpenTelemetry Batch Processor</seealso>
internal sealed class StructuredLogBatchProcessor : IDisposable
{
    private readonly IHub _hub;
    private readonly IClientReportRecorder _clientReportRecorder;
    private readonly IDiagnosticLogger? _diagnosticLogger;

    private readonly StructuredLogBatchBuffer _buffer1;
    private readonly StructuredLogBatchBuffer _buffer2;
    private volatile StructuredLogBatchBuffer _activeBuffer;

    public StructuredLogBatchProcessor(IHub hub, int batchCount, TimeSpan batchInterval, IClientReportRecorder clientReportRecorder, IDiagnosticLogger? diagnosticLogger)
    {
        _hub = hub;
        _clientReportRecorder = clientReportRecorder;
        _diagnosticLogger = diagnosticLogger;

        _buffer1 = new StructuredLogBatchBuffer(batchCount, batchInterval, OnTimeoutExceeded, "Buffer 1");
        _buffer2 = new StructuredLogBatchBuffer(batchCount, batchInterval, OnTimeoutExceeded, "Buffer 2");
        _activeBuffer = _buffer1;
    }

    internal void Enqueue(SentryLog log)
    {
        if (!_hub.IsEnabled)
        {
            return;
        }

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
        CaptureLogs(_buffer1);
        CaptureLogs(_buffer2);
    }

    /// <summary>
    /// Forces invocation of a Timeout of the active buffer.
    /// </summary>
    /// <remarks>
    /// Intended for Testing only.
    /// </remarks>
    internal void OnIntervalElapsed()
    {
        var activeBuffer = _activeBuffer;
        activeBuffer.OnIntervalElapsed(activeBuffer);
    }

    private bool TryEnqueue(StructuredLogBatchBuffer buffer, SentryLog log)
    {
        var status = buffer.Add(log);

        if (status is StructuredLogBatchBufferAddStatus.AddedLast)
        {
            SwapActiveBuffer(buffer);
            CaptureLogs(buffer);
            return true;
        }

        return status is StructuredLogBatchBufferAddStatus.AddedFirst or StructuredLogBatchBufferAddStatus.Added;
    }

    private void SwapActiveBuffer(StructuredLogBatchBuffer currentActiveBuffer)
    {
        var newActiveBuffer = ReferenceEquals(currentActiveBuffer, _buffer1) ? _buffer2 : _buffer1;
        _ = Interlocked.CompareExchange(ref _activeBuffer, newActiveBuffer, currentActiveBuffer);
    }

    private void CaptureLogs(StructuredLogBatchBuffer buffer)
    {
        SentryLog[]? logs = null;

        using (var scope = buffer.TryEnterFlushScope())
        {
            if (scope.IsEntered)
            {
                logs = scope.Flush();
            }
        }

        if (logs is not null && logs.Length != 0)
        {
            _ = _hub.CaptureEnvelope(Envelope.FromLog(new StructuredLog(logs)));
        }
    }

    private void OnTimeoutExceeded(StructuredLogBatchBuffer buffer)
    {
        if (!buffer.IsEmpty)
        {
            SwapActiveBuffer(buffer);
            CaptureLogs(buffer);
        }
    }

    public void Dispose()
    {
        _buffer1.Dispose();
        _buffer2.Dispose();
    }
}

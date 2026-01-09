using Sentry.Extensibility;

namespace Sentry.Internal;

/// <summary>
/// The Sentry Batch Processor.
/// </summary>
/// <remarks>
/// Uses a double buffer strategy to achieve synchronous and lock-free adding.
/// Switches the active buffer either when full or timeout exceeded (after first item added).
/// Items are dropped when both buffers are either full or being flushed.
/// Items are not enqueued when the Hub is disabled (Hub is being or has been disposed).
/// Flushing blocks the calling thread until all pending add operations have completed.
/// <code>
/// Implementation:
/// - When Hub is disabled (i.e. disposed), does not enqueue item
/// - Try to enqueue item into currently active buffer
///   - when currently active buffer is full, try to enqueue item into the other buffer
///   - when the other buffer is also full, or currently being flushed, then the item is dropped and a discarded event is recorded as a client report
/// - Swap currently active buffer when
///   - buffer is full
///   - timeout has exceeded
/// - Batch and Capture items after swapping currently active buffer
///   - wait until all pending add/enqueue operations have completed (required for timeout)
///   - flush the buffer and capture an envelope containing the batched items
/// - After flush, items can be enqueued again into the buffer
/// </code>
/// </remarks>
/// <seealso href="https://develop.sentry.dev/sdk/telemetry/spans/batch-processor/">Sentry Batch Processor</seealso>
/// <seealso href="https://github.com/open-telemetry/opentelemetry-collector/blob/main/processor/batchprocessor/README.md">OpenTelemetry Batch Processor</seealso>
/// <seealso href="https://develop.sentry.dev/sdk/telemetry/logs/">Sentry Logs</seealso>
/// <seealso href="https://develop.sentry.dev/sdk/telemetry/metrics/">Sentry Metrics</seealso>
internal sealed class BatchProcessor<TItem> : IDisposable
{
    private readonly IHub _hub;
    private readonly Action<IHub, TItem[]> _sendAction;
    private readonly IClientReportRecorder _clientReportRecorder;
    private readonly IDiagnosticLogger? _diagnosticLogger;

    private readonly BatchBuffer<TItem> _buffer1;
    private readonly BatchBuffer<TItem> _buffer2;
    private volatile BatchBuffer<TItem> _activeBuffer;

    public BatchProcessor(IHub hub, int batchCount, TimeSpan batchInterval, Action<IHub, TItem[]> sendAction, IClientReportRecorder clientReportRecorder, IDiagnosticLogger? diagnosticLogger)
    {
        _hub = hub;
        _sendAction = sendAction;
        _clientReportRecorder = clientReportRecorder;
        _diagnosticLogger = diagnosticLogger;

        _buffer1 = new BatchBuffer<TItem>(batchCount, batchInterval, OnTimeoutExceeded, "Buffer 1");
        _buffer2 = new BatchBuffer<TItem>(batchCount, batchInterval, OnTimeoutExceeded, "Buffer 2");
        _activeBuffer = _buffer1;
    }

    internal void Enqueue(TItem item)
    {
        if (!_hub.IsEnabled)
        {
            return;
        }

        var activeBuffer = _activeBuffer;

        if (!TryEnqueue(activeBuffer, item))
        {
            activeBuffer = ReferenceEquals(activeBuffer, _buffer1) ? _buffer2 : _buffer1;
            if (!TryEnqueue(activeBuffer, item))
            {
                _clientReportRecorder.RecordDiscardedEvent(DiscardReason.Backpressure, DataCategory.Default, 1);
                _diagnosticLogger?.LogInfo("{0}-Buffer full ... dropping {0}", typeof(TItem).Name);
            }
        }
    }

    internal void Flush()
    {
        CaptureItems(_buffer1);
        CaptureItems(_buffer2);
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

    private bool TryEnqueue(BatchBuffer<TItem> buffer, TItem item)
    {
        var status = buffer.Add(item);

        if (status is BatchBufferAddStatus.AddedLast)
        {
            SwapActiveBuffer(buffer);
            CaptureItems(buffer);
            return true;
        }

        return status is BatchBufferAddStatus.AddedFirst or BatchBufferAddStatus.Added;
    }

    private void SwapActiveBuffer(BatchBuffer<TItem> currentActiveBuffer)
    {
        var newActiveBuffer = ReferenceEquals(currentActiveBuffer, _buffer1) ? _buffer2 : _buffer1;
        _ = Interlocked.CompareExchange(ref _activeBuffer, newActiveBuffer, currentActiveBuffer);
    }

    private void CaptureItems(BatchBuffer<TItem> buffer)
    {
        TItem[]? items = null;

        using (var scope = buffer.TryEnterFlushScope())
        {
            if (scope.IsEntered)
            {
                items = scope.Flush();
            }
        }

        if (items is not null && items.Length != 0)
        {
            _sendAction(_hub, items);
        }
    }

    private void OnTimeoutExceeded(BatchBuffer<TItem> buffer)
    {
        if (!buffer.IsEmpty)
        {
            SwapActiveBuffer(buffer);
            CaptureItems(buffer);
        }
    }

    public void Dispose()
    {
        _buffer1.Dispose();
        _buffer2.Dispose();
    }
}

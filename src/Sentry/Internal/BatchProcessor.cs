using Sentry.Extensibility;
using Sentry.Infrastructure;
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
    private readonly IClientReportRecorder _clientReportRecorder;
    private readonly IDiagnosticLogger? _diagnosticLogger;

    private readonly BatchBuffer<SentryLog> _buffer1;
    private readonly BatchBuffer<SentryLog> _buffer2;
    private volatile BatchBuffer<SentryLog> _activeBuffer;

    public BatchProcessor(IHub hub, int batchCount, TimeSpan batchInterval, ISystemClock clock, IClientReportRecorder clientReportRecorder, IDiagnosticLogger? diagnosticLogger)
    {
        _hub = hub;
        _clientReportRecorder = clientReportRecorder;
        _diagnosticLogger = diagnosticLogger;

        _buffer1 = new BatchBuffer<SentryLog>(batchCount, batchInterval, clock, OnTimeoutExceeded, "Buffer 1");
        _buffer2 = new BatchBuffer<SentryLog>(batchCount, batchInterval, clock, OnTimeoutExceeded, "Buffer 2");
        _activeBuffer = _buffer1;
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
        CaptureLogs(_buffer1);
        CaptureLogs(_buffer2);
    }

    internal void OnIntervalElapsed()
    {
        var activeBuffer = _activeBuffer;
        activeBuffer.OnIntervalElapsed(activeBuffer);
    }

    private bool TryEnqueue(BatchBuffer<SentryLog> buffer, SentryLog log)
    {
        var status = buffer.Add(log);

        if (status is BatchBufferAddStatus.AddedLast)
        {
            TrySwapActiveBuffer(buffer);
            CaptureLogs(buffer);
            return true;
        }

        return status is BatchBufferAddStatus.AddedFirst or BatchBufferAddStatus.Added;
    }

    private bool TrySwapActiveBuffer(BatchBuffer<SentryLog> currentActiveBuffer)
    {
        var newActiveBuffer = ReferenceEquals(currentActiveBuffer, _buffer1) ? _buffer2 : _buffer1;
        var previousActiveBuffer = Interlocked.CompareExchange(ref _activeBuffer, newActiveBuffer, currentActiveBuffer);
        return previousActiveBuffer == currentActiveBuffer;
    }

    private void CaptureLogs(BatchBuffer<SentryLog> buffer)
    {
        SentryLog[]? logs = null;

        using (var scope = buffer.TryEnterFlushScope())
        {
            if (scope.IsEntered)
            {
                logs = scope.Flush();
            }
        }

        if (logs is not null)
        {
            _ = _hub.CaptureEnvelope(Envelope.FromLog(new StructuredLog(logs)));
        }
    }

    private void OnTimeoutExceeded(BatchBuffer<SentryLog> buffer, DateTimeOffset signalTime)
    {
        if (!buffer.IsEmpty)
        {
            TrySwapActiveBuffer(buffer);
            CaptureLogs(buffer);
        }
    }

    public void Dispose()
    {
        _buffer1.Dispose();
        _buffer2.Dispose();
    }
}

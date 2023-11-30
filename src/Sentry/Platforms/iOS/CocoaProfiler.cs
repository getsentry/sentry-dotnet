using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;
using Sentry.iOS.Extensions;
using Sentry.iOS.Facades;

namespace Sentry.iOS;

internal class CocoaProfiler : ITransactionProfiler
{
    private readonly SentryOptions _options;
    private readonly SentryId _traceId;
    private readonly CocoaSdk.SentryId _cocoaTraceId;
    private readonly ulong _starTimeNs;
    private ulong _endTimeNs;
    private readonly SentryStopwatch _stopwatch = SentryStopwatch.StartNew();

    public CocoaProfiler(SentryOptions options, ulong starTimeNs, SentryId traceId, CocoaSdk.SentryId cocoaTraceId)
    {
        _options = options;
        _starTimeNs = starTimeNs;
        _traceId = traceId;
        _cocoaTraceId = cocoaTraceId;
        _options.LogDebug("Trace {0} profile start timestamp: {1} ns", _traceId, _starTimeNs);
    }

    /// <inheritdoc />
    public void Finish()
    {
        if (_endTimeNs == 0)
        {
            _endTimeNs = _starTimeNs + (ulong)_stopwatch.ElapsedNanoseconds;
            _options.LogDebug("Trace {0} profile end timestamp: {1} ns", _traceId, _endTimeNs);
        }
    }

    public object Collect(Transaction transaction)
    {
        var payload = SentryCocoaHybridSdk.CollectProfileBetween(_starTimeNs, _endTimeNs, _cocoaTraceId);
        ArgumentNullException.ThrowIfNull(payload, "profile payload");

        _options.LogDebug("Trace {0} profile payload collected", _traceId);
        return new SerializableNSObject(payload);
    }
}

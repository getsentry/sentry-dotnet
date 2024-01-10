using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Cocoa.Extensions;
using Sentry.Cocoa.Facades;

namespace Sentry.Cocoa;

internal class CocoaProfiler : ITransactionProfiler
{
    private readonly SentryOptions _options;
    private readonly SentryId _traceId;
    private readonly CocoaSdk.SentryId _cocoaTraceId;
    private readonly ulong _startTimeNs;
    private ulong _endTimeNs;
    private readonly SentryStopwatch _stopwatch;

    public CocoaProfiler(SentryOptions options, ulong startTimeNs, SentryId traceId, CocoaSdk.SentryId cocoaTraceId)
    {
        _stopwatch = SentryStopwatch.StartNew();
        _options = options;
        _startTimeNs = startTimeNs;
        _traceId = traceId;
        _cocoaTraceId = cocoaTraceId;
        _options.LogDebug("Trace {0} profile start timestamp: {1} ns", _traceId, _startTimeNs);
    }

    /// <inheritdoc />
    public void Finish()
    {
        if (_endTimeNs == 0)
        {
            _endTimeNs = _startTimeNs + (ulong)_stopwatch.ElapsedNanoseconds;
            _options.LogDebug("Trace {0} profile end timestamp: {1} ns", _traceId, _endTimeNs);
        }
    }

    public ISerializable? Collect(SentryTransaction transaction)
    {
        var payload = SentryCocoaHybridSdk.CollectProfileBetween(_startTimeNs, _endTimeNs, _cocoaTraceId);
        if (payload is null)
        {
            _options.LogWarning("Trace {0} collected profile payload is null", _traceId);
            return null;
        }
        _options.LogDebug("Trace {0} profile payload collected", _traceId);

        var payloadTx = payload["transaction"]?.MutableCopy() as NSMutableDictionary;
        if (payloadTx is null)
        {
            _options.LogWarning("Trace {0} collected profile payload doesn't have transaction information", _traceId);
            return null;
        }

        payloadTx["id"] = transaction.EventId.ToString().ToNSString();
        payloadTx["trace_id"] = _traceId.ToString().ToNSString();
        payloadTx["name"] = transaction.Name.ToNSString();
        payload["transaction"] = payloadTx;
        payload["timestamp"] = transaction.StartTimestamp.ToString("o", CultureInfo.InvariantCulture).ToNSString();
        return new SerializableNSObject(payload);
    }
}

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
    private readonly SentryStopwatch _stopwatch = SentryStopwatch.StartNew();

    public CocoaProfiler(SentryOptions options, ulong startTimeNs, SentryId traceId, CocoaSdk.SentryId cocoaTraceId)
    {
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

    public ISerializable Collect(Transaction transaction)
    {
        // TODO change return type of CocoaSDKs CollectProfileBetween to NSMutableDictionary
        var payload = SentryCocoaHybridSdk.CollectProfileBetween(_startTimeNs, _endTimeNs, _cocoaTraceId)?.MutableCopy() as NSMutableDictionary;
        _options.LogDebug("Trace {0} profile payload collected", _traceId);

        ArgumentNullException.ThrowIfNull(payload, "profile payload");
        payload["timestamp"] = transaction.StartTimestamp.ToString("o", CultureInfo.InvariantCulture).ToNSString();

        var payloadTx = payload["transaction"]?.MutableCopy() as NSMutableDictionary;
        ArgumentNullException.ThrowIfNull(payloadTx, "profile payload transaction");
        payloadTx["id"] = transaction.EventId.ToString().ToNSString();
        payloadTx["trace_id"] = _traceId.ToString().ToNSString();
        payloadTx["name"] = transaction.Name.ToNSString();
        payload["transaction"] = payloadTx;
        return new SerializableNSObject(payload);
    }
}

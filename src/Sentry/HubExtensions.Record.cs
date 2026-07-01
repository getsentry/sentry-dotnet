using Sentry.Internal;

namespace Sentry;

public static partial class HubExtensions
{
    /// <summary>
    /// Records a transaction that has already completed elsewhere — for example, spans measured on another
    /// machine or process and replayed through a proxy. Timing is supplied explicitly rather than measured
    /// live, so unlike <see cref="StartTransaction(IHub, string, string)"/> there is no stopwatch, idle timer,
    /// or sampling decision involved; the transaction is materialized and captured once when
    /// <paramref name="configure"/> returns.
    /// </summary>
    /// <param name="hub">The hub.</param>
    /// <param name="name">The transaction name.</param>
    /// <param name="operation">The transaction operation.</param>
    /// <param name="startTimestamp">When the transaction started.</param>
    /// <param name="duration">How long the transaction ran. Must not be negative.</param>
    /// <param name="traceId">Optional trace id to preserve from the originating system; generated when omitted.</param>
    /// <param name="spanId">Optional root span id to preserve; generated when omitted.</param>
    /// <param name="parentSpanId">Optional parent span id, when continuing a trace from another service.</param>
    /// <param name="configure">
    /// Optional callback to set metadata and record the span tree via
    /// <see cref="ISpanRecorder.RecordSpan(string, DateTimeOffset, TimeSpan, SpanId?, Action{ISpanRecorder}?)"/>.
    /// </param>
    /// <returns>The event id of the captured transaction.</returns>
    public static SentryId RecordTransaction(
        this IHub hub,
        string name,
        string operation,
        DateTimeOffset startTimestamp,
        TimeSpan duration,
        SentryId? traceId = null,
        SpanId? spanId = null,
        SpanId? parentSpanId = null,
        Action<ITransactionRecorder>? configure = null)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Transaction duration cannot be negative.");
        }

        var context = new TransactionContext(
            name,
            operation,
            spanId: spanId,
            parentSpanId: parentSpanId,
            traceId: traceId,
            isSampled: true);

        var tracer = new TransactionTracer(hub, context)
        {
            StartTimestamp = startTimestamp,
            EndTimestamp = startTimestamp + duration,
        };
        tracer.Status ??= SpanStatus.Ok;

        var recorder = new TransactionRecorder(tracer);
        configure?.Invoke(recorder);

        var transaction = new SentryTransaction(tracer);

        // A recorded transaction represents work that happened elsewhere, so we don't want the current
        // (live) scope's breadcrumbs/user/tags/contexts leaking onto it. Capture against a clean scope when
        // we have options to construct one; otherwise fall back (e.g. a disabled hub, where this is a no-op).
        var options = hub.GetSentryOptions();
        if (options is not null)
        {
            hub.CaptureTransaction(transaction, new Scope(options), null);
        }
        else
        {
            hub.CaptureTransaction(transaction);
        }

        return transaction.EventId;
    }
}

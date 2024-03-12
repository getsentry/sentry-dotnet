using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry;

/// <summary>
/// Transaction span tracer.
/// </summary>
public class SpanTracer : TransactionTracer
{
    /// <summary>
    /// Initializes an instance of <see cref="SpanTracer"/>.
    /// </summary>
    public SpanTracer(
        IHub hub,
        TransactionTracer rootSpan,
        SpanId? parentSpanId,
        SentryId traceId,
        string operation)
    : base(hub, rootSpan, null, parentSpanId, traceId, operation)
    {
    }

    internal SpanTracer(
        IHub hub,
        // This is a bit counter-intuitive... it'd be easier to pass in the parent span and infer the root span from
        // this but our previous API assumed a "Transaction" (i.e. root span) would be passed in so we're doing it this
        // way to preserve backward compatibility
        TransactionTracer rootSpan,
        SpanId? spanId,
        SpanId? parentSpanId,
        SentryId traceId,
        string operation)
    : base(hub, rootSpan, spanId, parentSpanId, traceId, operation)
    {
    }
}

namespace Sentry.Protocol;

/// <summary>
/// Trace metadata stored in 'contexts.trace' on an event or transaction.
/// </summary>
public interface ITraceContext
{
    /// <summary>
    /// Span ID.
    /// </summary>
    SpanId SpanId { get; }

    /// <summary>
    /// Parent ID.
    /// </summary>
    SpanId? ParentSpanId { get; }

    /// <summary>
    /// Trace ID.
    /// </summary>
    SentryId TraceId { get; }

    /// <summary>
    /// Operation.
    /// </summary>
    string Operation { get; }

    /// <summary>
    /// Specifies the origin of the trace. If no origin is set then the trace origin is assumed to be "manual".
    /// </summary>
    /// <remarks>
    /// See https://develop.sentry.dev/sdk/performance/trace-origin/ for more information.
    /// </remarks>
    string? Origin { get; }

    /// <summary>
    /// Description.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Status.
    /// </summary>
    SpanStatus? Status { get; }

    // Note: this may need to be mutated internally,
    // but the user should never be able to change it
    // on their own.

    /// <summary>
    /// Whether the span or transaction is sampled in (i.e. eligible for sending to Sentry).
    /// </summary>
    bool? IsSampled { get; }
}

namespace Sentry.Protocol;

/// <summary>
/// Trace metadata stored in 'contexts.trace' on an event or transaction.
/// </summary>
public interface ITraceContext
{
    /// <summary>
    /// Span ID.
    /// </summary>
    public SpanId SpanId { get; }

    /// <summary>
    /// Parent ID.
    /// </summary>
    public SpanId? ParentSpanId { get; }

    /// <summary>
    /// Trace ID.
    /// </summary>
    public SentryId TraceId { get; }

    /// <summary>
    /// Operation.
    /// </summary>
    public string Operation { get; }

    /// <summary>
    /// Specifies the origin of the trace. If no origin is set then the trace origin is assumed to be "manual".
    /// </summary>
    /// <remarks>
    /// See https://develop.sentry.dev/sdk/performance/trace-origin/ for more information.
    /// </remarks>
    public string? Origin { get; }

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Status.
    /// </summary>
    public SpanStatus? Status { get; }

    // Note: this may need to be mutated internally,
    // but the user should never be able to change it
    // on their own.

    /// <summary>
    /// Whether the span or transaction is sampled in (i.e. eligible for sending to Sentry).
    /// </summary>
    public bool? IsSampled { get; }
}

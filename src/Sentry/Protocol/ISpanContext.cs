namespace Sentry.Protocol
{
    // Parts of transaction (which is a span) are stored in a context
    // for some unknown reason. This interface defines those fields.

    /// <summary>
    /// Span metadata.
    /// </summary>
    public interface ISpanContext
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
        /// Status.
        /// </summary>
        SpanStatus? Status { get; }

        /// <summary>
        /// Is sampled.
        /// </summary>
        bool IsSampled { get; set; }
    }
}

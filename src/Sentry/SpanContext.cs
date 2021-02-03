namespace Sentry
{
    /// <summary>
    /// Span metadata used for sampling.
    /// </summary>
    public class SpanContext : ISpanContext
    {
        /// <inheritdoc />
        public SpanId SpanId { get; }

        /// <inheritdoc />
        public SpanId? ParentSpanId { get; }

        /// <inheritdoc />
        public SentryId TraceId { get; }

        /// <inheritdoc />
        public string Operation { get; }

        /// <inheritdoc />
        public string? Description { get; }

        /// <inheritdoc />
        public SpanStatus? Status { get; }

        /// <inheritdoc />
        public bool? IsSampled { get; }

        /// <summary>
        /// Initializes an instance of <see cref="SpanContext"/>.
        /// </summary>
        public SpanContext(
            SpanId spanId,
            SpanId? parentSpanId,
            SentryId traceId,
            string operation,
            string? description,
            SpanStatus? status,
            bool? isSampled)
        {
            SpanId = spanId;
            ParentSpanId = parentSpanId;
            TraceId = traceId;
            Operation = operation;
            Description = description;
            Status = status;
            IsSampled = isSampled;
        }
    }
}

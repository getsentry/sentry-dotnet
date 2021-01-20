namespace Sentry.Protocol
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
        public SpanStatus? Status { get; set; }

        /// <inheritdoc />
        public bool? IsSampled { get; }

        /// <inheritdoc />
        public string? Description { get; set; }

        /// <summary>
        /// Initializes an instance of <see cref="SpanContext"/>.
        /// </summary>
        public SpanContext(
            SpanId spanId,
            SpanId? parentSpanId,
            SentryId traceId,
            string operation,
            bool? isSampled)
        {
            SpanId = spanId;
            ParentSpanId = parentSpanId;
            TraceId = traceId;
            Operation = operation;
            IsSampled = isSampled;
        }
    }
}

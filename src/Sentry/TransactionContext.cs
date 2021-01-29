namespace Sentry
{
    /// <summary>
    /// Transaction metadata used for sampling.
    /// </summary>
    public class TransactionContext : SpanContext, ITransactionContext
    {
        /// <inheritdoc />
        public string Name { get; }

        /// <summary>
        /// Initializes an instance of <see cref="TransactionContext"/>.
        /// </summary>
        public TransactionContext(
            SpanId spanId,
            SpanId? parentSpanId,
            SentryId traceId,
            string name,
            string operation,
            string description,
            SpanStatus? status,
            bool? isSampled)
            : base(spanId, parentSpanId, traceId, operation, description, status, isSampled)
        {
            Name = name;
        }

        /// <summary>
        /// Initializes an instance of <see cref="TransactionContext"/>.
        /// </summary>
        public TransactionContext(
            SpanId? parentSpanId,
            SentryId traceId,
            string name,
            string operation,
            bool? isSampled)
            : this(SpanId.Create(), parentSpanId, traceId, name, operation, "", null, isSampled)
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="TransactionContext"/>.
        /// </summary>
        public TransactionContext(
            string name,
            string operation,
            bool? isSampled)
            : this(null, SentryId.Create(), name, operation, isSampled)
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="TransactionContext"/>.
        /// </summary>
        public TransactionContext(string name, string operation)
            : this(name, operation, null)
        {
        }
    }
}

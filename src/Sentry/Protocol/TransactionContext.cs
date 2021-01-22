﻿namespace Sentry.Protocol
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
            string name,
            string operation,
            string description,
            bool? isSampled)
            : this(SpanId.Create(), null, SentryId.Create(), name, operation, description, null, isSampled)
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="TransactionContext"/>.
        /// </summary>
        public TransactionContext(
            string name,
            string operation,
            bool? isSampled)
            : this(name, operation, "", isSampled)
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

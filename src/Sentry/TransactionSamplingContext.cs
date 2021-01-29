using System.Collections.Generic;

namespace Sentry
{
    /// <summary>
    /// Context used by a dynamic sampler to determine whether a transaction should be sampled.
    /// </summary>
    public class TransactionSamplingContext
    {
        /// <summary>
        /// Transaction context.
        /// </summary>
        public ITransactionContext TransactionContext { get; }

        /// <summary>
        /// Custom data used for sampling.
        /// </summary>
        public IReadOnlyDictionary<string, object?> CustomSamplingContext { get; }

        /// <summary>
        /// Initializes an instance of <see cref="TransactionSamplingContext"/>.
        /// </summary>
        public TransactionSamplingContext(
            ITransactionContext transactionContext,
            IReadOnlyDictionary<string, object?> customSamplingContext)
        {
            TransactionContext = transactionContext;
            CustomSamplingContext = customSamplingContext;
        }
    }
}

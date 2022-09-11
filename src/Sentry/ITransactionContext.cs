namespace Sentry
{
    /// <summary>
    /// Transaction metadata.
    /// </summary>
    public interface ITransactionContext : ISpanContext
    {
        /// <summary>
        /// Transaction name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// TransactionSourceType.
        /// </summary>
        TransactionNameSource? Source { get; }

        /// <summary>
        /// Whether the parent transaction of this transaction has been sampled.
        /// </summary>
        bool? IsParentSampled { get; }
    }
}

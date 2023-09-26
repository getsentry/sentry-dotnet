namespace Sentry;

/// <summary>
/// Transaction.
/// </summary>
public interface ITransaction : ITransactionData, ISpan
{
    /// <summary>
    /// Transaction name.
    /// </summary>
    // 'new' because it adds a setter
    new string Name { get; set; }

    /// <summary>
    /// Whether the parent transaction of this transaction has been sampled.
    /// </summary>
    // 'new' because it adds a setter
    new bool? IsParentSampled { get; set; }

    /// <summary>
    /// The source of the transaction name.
    /// </summary>
    TransactionNameSource NameSource { get; }

    /// <summary>
    /// Flat list of spans within this transaction.
    /// </summary>
    IReadOnlyCollection<ISpan> Spans { get; }

    /// <summary>
    /// Gets the last active (not finished) span in this transaction.
    /// </summary>
    ISpan? GetLastActiveSpan();
}

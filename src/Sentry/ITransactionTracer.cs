namespace Sentry;

/// <summary>
/// TransactionTracer interface
/// </summary>
public interface ITransactionTracer : ITransactionData, ISpan
{
    /// <summary>
    /// Transaction name.
    /// </summary>
    // 'new' because it adds a setter
    public new string Name { get; set; }

    /// <summary>
    /// Whether the parent transaction of this transaction has been sampled.
    /// </summary>
    // 'new' because it adds a setter
    public new bool? IsParentSampled { get; set; }

    /// <summary>
    /// Flat list of spans within this transaction.
    /// </summary>
    public IReadOnlyCollection<ISpan> Spans { get; }

    /// <summary>
    /// Gets the last active (not finished) span in this transaction.
    /// </summary>
    public ISpan? GetLastActiveSpan();
}

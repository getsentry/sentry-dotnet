using Sentry.Protocol;

namespace Sentry;

/// <summary>
/// Transaction metadata.
/// </summary>
public interface ITransactionContext : ITraceContext
{
    /// <summary>
    /// Transaction name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Whether the parent transaction of this transaction has been sampled.
    /// </summary>
    public bool? IsParentSampled { get; }

    /// <summary>
    /// The source of the transaction name.
    /// </summary>
    public TransactionNameSource NameSource { get; }
}

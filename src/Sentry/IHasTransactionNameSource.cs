namespace Sentry;

/// <summary>
/// Interface for transactions that implement a transaction name source.
/// </summary>
/// <remarks>
/// Ideally, this would just be implemented as part of <see cref="ITransaction"/> and <see cref="ITransactionContext"/>.
/// However, adding a property to a public interface is a breaking change.  We can do that in a future major version.
/// </remarks>
public interface IHasTransactionNameSource
{
    /// <summary>
    /// The source of the transaction name.
    /// </summary>
    TransactionNameSource NameSource { get; }
}

namespace Sentry;

/// <summary>
/// Immutable data belonging to a transaction.
/// </summary>
public interface ITransactionData : ISpanData, ITransactionContext, IEventLike
{
}

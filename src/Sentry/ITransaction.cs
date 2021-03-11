namespace Sentry
{
    /// <summary>
    /// Transaction.
    /// </summary>
    public interface ITransaction : ISpan, ITransactionContext, IEventLike
    {
    }
}

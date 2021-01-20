namespace Sentry.Protocol
{
    public interface ITransactionContext : ISpanContext
    {
        /// <summary>
        /// Transaction name.
        /// </summary>
        string Name { get; }
    }
}

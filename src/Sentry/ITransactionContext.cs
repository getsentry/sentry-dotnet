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
    }
}

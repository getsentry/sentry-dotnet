namespace Sentry.Protocol
{
    /// <summary>
    /// Transaction.
    /// </summary>
    public interface ITransaction : ISpan, IEventLike
    {
        /// <summary>
        /// Transaction event ID.
        /// </summary>
        SentryId EventId { get; }

        /// <summary>
        /// Transaction name.
        /// </summary>
        string Name { get; set; }
    }
}

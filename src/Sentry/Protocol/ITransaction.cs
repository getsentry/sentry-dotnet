using System.Collections.Generic;

namespace Sentry.Protocol
{
    /// <summary>
    /// Transaction.
    /// </summary>
    public interface ITransaction : ISpan, ITransactionContext, IEventLike
    {
        /// <summary>
        /// Transaction event ID.
        /// </summary>
        SentryId EventId { get; }

        /// <summary>
        /// Transaction name.
        /// </summary>
        // 'new' because it adds a setter
        new string Name { get; set; }

        // Should this be on IEventLike?
        /// <summary>
        /// The release version of the application.
        /// </summary>
        string? Release { get; set; }

        /// <summary>
        /// Flat list of spans within this transaction.
        /// </summary>
        IReadOnlyCollection<ISpan> Spans { get; }

        /// <summary>
        /// Gets the last active (not finished) span in this transaction.
        /// </summary>
        ISpan? GetLastActiveSpan();
    }
}

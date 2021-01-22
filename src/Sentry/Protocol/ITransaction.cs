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

        /// <summary>
        /// Flat list of spans within this transaction.
        /// </summary>
        IReadOnlyCollection<ISpan> Spans { get; }

        /// <summary>
        /// Get Sentry trace header.
        /// </summary>
        SentryTraceHeader GetTraceHeader();
    }
}

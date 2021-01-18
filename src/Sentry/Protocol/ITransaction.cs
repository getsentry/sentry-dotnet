using System.Collections.Generic;

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

        /// <summary>
        /// Flat list of spans within this transaction.
        /// </summary>
        IReadOnlyList<Span> Spans { get; }

        /// <summary>
        /// Get Sentry trace header.
        /// </summary>
        SentryTraceHeader GetTraceHeader();
    }
}

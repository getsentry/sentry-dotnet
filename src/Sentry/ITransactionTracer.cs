using System.Collections.Generic;

namespace Sentry
{
    public interface ITransactionTracer : ITransaction, ISpanTracer
    {
        /// <summary>
        /// Transaction name.
        /// </summary>
        // 'new' because it adds a setter
        new string Name { get; set; }

        /// <summary>
        /// Flat list of spans within this transaction.
        /// </summary>
        IReadOnlyCollection<ISpanTracer> Spans { get; }

        /// <summary>
        /// Gets the last active (not finished) span in this transaction.
        /// </summary>
        ISpanTracer? GetLastActiveSpan();
    }
}

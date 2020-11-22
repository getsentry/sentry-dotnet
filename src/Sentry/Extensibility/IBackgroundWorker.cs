using System;
using System.Threading.Tasks;
using Sentry.Protocol.Envelopes;

namespace Sentry.Extensibility
{
    /// <summary>
    /// A worker that queues envelopes synchronously and flushes async.
    /// </summary>
    internal interface IBackgroundWorker
    {
        /// <summary>
        /// Attempts to queue the envelope with the worker.
        /// </summary>
        /// <returns>True of queueing was successful. Otherwise, false.</returns>
        bool EnqueueEnvelope(Envelope envelope);

        /// <summary>
        /// Flushes envelopes asynchronously.
        /// </summary>
        /// <param name="timeout">How long to wait for flush to finish.</param>
        /// <returns>A task to await for the flush operation.</returns>
        Task FlushAsync(TimeSpan timeout);

        /// <summary>
        /// Current count of items queued up.
        /// </summary>
        int QueuedItems { get; }
    }
}

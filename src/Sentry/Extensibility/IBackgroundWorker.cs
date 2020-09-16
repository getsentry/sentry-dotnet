using System;
using System.Threading.Tasks;

namespace Sentry.Extensibility
{
    /// <summary>
    /// A worker that queues event synchronously and flushes async.
    /// </summary>
    public interface IBackgroundWorker
    {
        /// <summary>
        /// Attempts to queue the event with the worker.
        /// </summary>
        /// <returns>True of queueing was successful. Otherwise, false.</returns>
        bool EnqueueEvent(SentryEvent @event);

        /// <summary>
        /// Flushes events asynchronously.
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

using Sentry.Protocol.Envelopes;

namespace Sentry.Extensibility;

/// <summary>
/// A worker that queues envelopes synchronously and flushes async.
/// </summary>
public interface IBackgroundWorker
{
    /// <summary>
    /// Attempts to enqueue the envelope with the worker.
    /// </summary>
    /// <param name="envelope">The envelope to enqueue.</param>
    /// <returns>True of queueing was successful. Otherwise, false.</returns>
    public bool EnqueueEnvelope(Envelope envelope);

    /// <summary>
    /// Flushes envelopes asynchronously.
    /// </summary>
    /// <param name="timeout">How long to wait for flush to finish.</param>
    /// <returns>A task to await for the flush operation.</returns>
    public Task FlushAsync(TimeSpan timeout);

    /// <summary>
    /// Current count of items queued up.
    /// </summary>
    public int QueuedItems { get; }
}

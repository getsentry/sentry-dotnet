using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;

namespace Sentry.Internal
{
    internal class BackgroundWorker : IBackgroundWorker, IDisposable
    {
        private readonly SentryOptions _options;
        private readonly IProducerConsumerCollection<SentryEvent> _queue;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly SemaphoreSlim _inSemaphore;
        private readonly SemaphoreSlim _outSemaphore;
        private volatile bool _disposed;

        internal Task WorkerTask { get; }

        public int QueuedItems => _queue.Count;

        public BackgroundWorker(
            ITransport transport,
            SentryOptions options)
        : this(transport, options, null, null)
        { }

        internal BackgroundWorker(
            ITransport transport,
            SentryOptions options,
            CancellationTokenSource cancellationTokenSource = null,
            IProducerConsumerCollection<SentryEvent> queue = null)
        {
            Debug.Assert(transport != null);
            Debug.Assert(options != null);

            _inSemaphore = new SemaphoreSlim(options.MaxQueueItems, options.MaxQueueItems);
            _outSemaphore = new SemaphoreSlim(0, options.MaxQueueItems);
            _options = options;

            _cancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();
            _queue = queue ?? new ConcurrentQueue<SentryEvent>();

            WorkerTask = Task.Run(
                async () => await WorkerAsync(
                    _queue,
                    _options,
                    transport,
                    _inSemaphore,
                    _outSemaphore,
                    _cancellationTokenSource.Token)
                    .ConfigureAwait(false));
        }

        public bool EnqueueEvent(SentryEvent @event)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(BackgroundWorker));
            }

            if (@event == null)
            {
                return false;
            }

            var acquired = _inSemaphore.Wait(TimeSpan.Zero);
            if (acquired)
            {
                _queue.TryAdd(@event);
                _outSemaphore.Release();
            }
            return acquired;
        }

        private static async Task WorkerAsync(
           IProducerConsumerCollection<SentryEvent> queue,
           SentryOptions options,
           ITransport transport,
           SemaphoreSlim inSemaphore,
           SemaphoreSlim outSemaphore,
           CancellationToken cancellation)
        {
            var shutdownTimeout = new CancellationTokenSource();
            var shutdownRequested = false;

            try
            {
                while (!shutdownTimeout.IsCancellationRequested)
                {
                    // If the cancellation was signaled,
                    // set the latest we can keep reading off of the queue (while there's still stuff to read)
                    // No longer synchronized with outSemaphore (Enqueue will throw object disposed),
                    // run until the end of the queue or shutdownTimeout
                    if (!shutdownRequested)
                    {
                        try
                        {
                            await outSemaphore.WaitAsync(cancellation).ConfigureAwait(false);
                        }
                        // Cancellation requested, scheduled shutdown but continue in case there are more items
                        catch (OperationCanceledException)
                        {
                            if (options.ShutdownTimeout == TimeSpan.Zero)
                            {
                                options.DiagnosticLogger?.LogDebug("Exiting immediately due to 0 shutdown timeout. #{0} in queue.", queue.Count);
                                return;
                            }
                            else
                            {
                                options.DiagnosticLogger?.LogDebug("Shutdown scheduled. Stopping by: {0}. #{1} in queue.", options.ShutdownTimeout, queue.Count);
                                shutdownTimeout.CancelAfter(options.ShutdownTimeout);
                            }

                            shutdownRequested = true;
                        }
                    }

                    if (queue.TryTake(out var @event))
                    {
                        inSemaphore.Release();
                        try
                        {
                            // Optionally we can keep multiple requests in-flight concurrently:
                            // instead of awaiting here, keep reading from the queue while less than
                            // N events are being sent
                            var task = transport.CaptureEventAsync(@event, shutdownTimeout.Token).ConfigureAwait(false);
                            options.DiagnosticLogger?.LogDebug("Event {0} in-flight to Sentry. #{1} in queue.", @event.EventId, queue.Count);
                            await task;
                        }
                        catch (OperationCanceledException)
                        {
                            options.DiagnosticLogger?.LogInfo("Shutdown token triggered. Time to exit. #{0} in queue.", queue.Count);
                            return;
                        }
                        catch (Exception exception)
                        {
                            options.DiagnosticLogger?.LogError("Error while processing event {1}: {0}. #{2} in queue.", exception, @event.EventId, queue.Count);
                        }
                    }
                    else
                    {
                        Debug.Assert(shutdownRequested);
                        options.DiagnosticLogger?.LogInfo("Exiting the worker with an empty queue.");

                        // Empty queue. Exit.
                        return;
                    }
                }
            }
            finally
            {
                inSemaphore.Dispose();
                outSemaphore.Dispose();
            }
        }

        /// <summary>
        /// Stops the background worker and waits for it to empty the queue until 'shutdownTimeout' is reached
        /// </summary>
        /// <inheritdoc />
        public void Dispose()
        {
            _options.DiagnosticLogger?.LogDebug("Disposing BackgroundWorker.");

            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                // Immediately requests the Worker to stop.
                _cancellationTokenSource.Cancel();

                // If there's anything in the queue, it'll keep running until 'shutdownTimeout' is reached
                // If the queue is empty it will quit immediately
                WorkerTask.Wait(_options.ShutdownTimeout);
            }
            catch (Exception exception)
            {
                _options.DiagnosticLogger?.LogError("Stopping the background worker threw an exception.", exception);
            }

            if (_queue.Count > 0)
            {
                _options.DiagnosticLogger?.LogWarning("Worker stopped while {0} were still in the queue.", _queue.Count);
            }
        }
    }
}

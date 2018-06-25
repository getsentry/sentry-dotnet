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
        private readonly BackgroundWorkerOptions _options;
        private readonly IProducerConsumerCollection<SentryEvent> _queue;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly SemaphoreSlim _inSemaphore;
        private readonly SemaphoreSlim _outSemaphore;
        private bool _disposed;

        internal Task WorkerTask { get; }

        public int QueuedItems => _queue.Count;

        public BackgroundWorker(
            ITransport transport,
            BackgroundWorkerOptions options)
        : this(transport, options, null, null)
        { }

        internal BackgroundWorker(
            ITransport transport,
            BackgroundWorkerOptions options,
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

            var acquired = _inSemaphore.Wait((int)_options.FullQueueBlockTimeout.TotalMilliseconds);
            if (acquired)
            {
                _queue.TryAdd(@event);
                _outSemaphore.Release();
            }
            return acquired;
        }

        private static async Task WorkerAsync(
           IProducerConsumerCollection<SentryEvent> queue,
           BackgroundWorkerOptions options,
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
                                return;
                            }
                            else
                            {
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
                            await transport.CaptureEventAsync(@event, shutdownTimeout.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            // Shutdown token triggered. Time to exit.
                            return;
                        }
                        catch (Exception exception)
                        {
                            // TODO: Notify error handler
                            Debug.WriteLine(exception.ToString());
                        }
                    }
                    else
                    {
                        Debug.Assert(shutdownRequested);

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
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                // Immediately requests the Worker to stop.
                _cancellationTokenSource.Cancel();

                // If there's anything in the queue, it'll keep running until 'shutudownTimeout' is reached
                // If the queue is empty it will quit immediately
                WorkerTask.Wait(_options.ShutdownTimeout);
            }
            catch (Exception exception)
            {
                // TODO: Notify error handler
                Debug.WriteLine(exception.ToString());
            }

            if (_queue.Count > 0)
            {
                // TODO: Notify error handler
                Debug.WriteLine($"Worker stopped while {_queue.Count} were still in the queue.");
            }
        }
    }
}

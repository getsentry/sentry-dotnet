using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Extensibility.Http;

namespace Sentry.Internals
{
    internal class BackgroundWorker : IBackgroundWorker, IDisposable
    {
        private readonly BackgroundWorkerOptions _options;
        private readonly BlockingCollection<SentryEvent> _queue;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _disposed;

        internal Task WorkerTask;

        public int QueuedItems => _queue.Count;

        public BackgroundWorker(
            ITransport transport,
            BackgroundWorkerOptions options)
        : this(transport, options, null, null)
        { }

        internal BackgroundWorker(
            ITransport transport = null,
            BackgroundWorkerOptions options = null,
            CancellationTokenSource cancellationTokenSource = null,
            IProducerConsumerCollection<SentryEvent> queue = null)
        {
            transport = transport ?? new HttpTransport();
            _options = options ?? new BackgroundWorkerOptions();
            _cancellationTokenSource = cancellationTokenSource ?? new CancellationTokenSource();
            queue = queue ?? new ConcurrentQueue<SentryEvent>();

            _queue = new BlockingCollection<SentryEvent>(queue, _options.MaxQueueItems);

            WorkerTask = Task.Run(
                async () => await WorkerAsync(
                    _queue,
                    _options,
                    transport,
                    _cancellationTokenSource.Token)
                    .ConfigureAwait(false));
        }

        public bool EnqueueEvent(SentryEvent @event)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(BackgroundWorker));
            }

            return _queue.TryAdd(
                @event,
                (int)_options.FullQueueBlockTimeout.TotalMilliseconds);
        }

        private static async Task WorkerAsync(
           BlockingCollection<SentryEvent> queue,
           BackgroundWorkerOptions options,
           ITransport transport,
           CancellationToken cancellation)
        {
            var shutdownTimeout = new CancellationTokenSource();
            var shutdownRequested = false;

            while (!shutdownTimeout.IsCancellationRequested)
            {
                // If the cancellation was signaled, 
                // set the latest we can keep reading off the queue (while there's still stuff to read)
                if (!shutdownRequested && cancellation.IsCancellationRequested)
                {
                    shutdownTimeout.CancelAfter(options.ShutdownTimeout);
                    shutdownRequested = true;
                }

                if (queue.TryTake(out var @event))
                {
                    try
                    {
                        // Optionally we can keep multiple requests in-flight concurrently:
                        // instead of awaiting here, keep reading from the queue while less than
                        // N events are being sent
                        await transport.CaptureEventAsync(@event, cancellation).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        // TODO: Notify error handler
                        Trace.WriteLine(exception.ToString());
                    }
                }
                else
                {
                    if (shutdownRequested)
                    {
                        break; // Shutdown requested and queue is empty. ready to quit.
                    }

                    // Queue is empty, wait asynchronously before polling again
                    try
                    {
                        await Task.Delay(options.EmptyQueueDelay, cancellation).ConfigureAwait(false);
                    }
                    // Cancellation requested, scheduled shutdown but loop again in case there are more items
                    catch (OperationCanceledException)
                    {
                        shutdownTimeout.CancelAfter(options.ShutdownTimeout);
                        shutdownRequested = true;
                    }
                }
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
                try
                {
                    // If there's anything in the queue, it'll keep running until 'shutudownTimeout' is reached
                    // If the queue is empty it will quit immediately
                    WorkerTask.GetAwaiter().GetResult();
                }
                catch (Exception exception)
                {
                    // TODO: Notify error handler
                    Trace.WriteLine(exception.ToString());
                }

                if (_queue.Count > 0)
                {
                    // TODO: Notify error handler
                    Trace.WriteLine($"Worker stopped while {_queue.Count} were still in the queue.");
                }
            }
            finally
            {
                _queue.Dispose();
            }
        }
    }
}

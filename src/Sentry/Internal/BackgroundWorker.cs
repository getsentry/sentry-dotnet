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
        private readonly ConcurrentQueue<SentryEvent> _queue;
        private readonly CancellationTokenSource _shutdownSource;
        private readonly SemaphoreSlim _queuedEventSemaphore;
        private volatile bool _disposed;
        private readonly int _maxItems;
        private int _currentItems;

        private event EventHandler OnFlushObjectReceived;

        internal Task WorkerTask { get; }

        public int QueuedItems => _queue.Count;

        public BackgroundWorker(
            ITransport transport,
            SentryOptions options)
            : this(transport, options, null, null)
        {
        }

        internal BackgroundWorker(
            ITransport transport,
            SentryOptions options,
            CancellationTokenSource shutdownSource = null,
            ConcurrentQueue<SentryEvent> queue = null)
        {
            Debug.Assert(transport != null);
            Debug.Assert(options != null);

            _maxItems = options.MaxQueueItems;

            _queuedEventSemaphore = new SemaphoreSlim(0, _maxItems);
            _options = options;

            _shutdownSource = shutdownSource ?? new CancellationTokenSource();
            _queue = queue ?? new ConcurrentQueue<SentryEvent>();

            WorkerTask = Task.Run(
                async () => await WorkerAsync(
                    _queue,
                    _options,
                    transport,
                    _queuedEventSemaphore,
                    _shutdownSource.Token)
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

            if (Interlocked.Increment(ref _currentItems) > _maxItems)
            {
                Interlocked.Decrement(ref _currentItems);
                return false;
            }

            _queue.Enqueue(@event);
            _queuedEventSemaphore.Release();
            return true;
        }

        private async Task WorkerAsync(
            ConcurrentQueue<SentryEvent> queue,
            SentryOptions options,
            ITransport transport,
            SemaphoreSlim queuedEventSemaphore,
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
                    // No longer synchronized with queuedEventSemaphore (Enqueue will throw object disposed),
                    // run until the end of the queue or shutdownTimeout
                    if (!shutdownRequested)
                    {
                        try
                        {
                            await queuedEventSemaphore.WaitAsync(cancellation).ConfigureAwait(false);
                        }
                        // Cancellation requested, scheduled shutdown but continue in case there are more items
                        catch (OperationCanceledException)
                        {
                            if (options.ShutdownTimeout == TimeSpan.Zero)
                            {
                                options.DiagnosticLogger?.LogDebug(
                                    "Exiting immediately due to 0 shutdown timeout. #{0} in queue.", queue.Count);
                                return;
                            }
                            else
                            {
                                options.DiagnosticLogger?.LogDebug(
                                    "Shutdown scheduled. Stopping by: {0}. #{1} in queue.", options.ShutdownTimeout,
                                    queue.Count);
                                shutdownTimeout.CancelAfter(options.ShutdownTimeout);
                            }

                            shutdownRequested = true;
                        }
                    }

                    if (queue.TryPeek(out var @event)) // Work with the event while it's in the queue
                    {
                        try
                        {
                            var task = transport.CaptureEventAsync(@event, shutdownTimeout.Token);
                            options.DiagnosticLogger?.LogDebug("Event {0} in-flight to Sentry. #{1} in queue.", @event.EventId, queue.Count);
                            await task.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            options.DiagnosticLogger?.LogInfo("Shutdown token triggered. Time to exit. #{0} in queue.",
                                queue.Count);
                            return;
                        }
                        catch (Exception exception)
                        {
                            options.DiagnosticLogger?.LogError("Error while processing event {1}: {0}. #{2} in queue.",
                                exception, @event.EventId, queue.Count);
                        }
                        finally
                        {
                            queue.TryDequeue(out _);
                            Interlocked.Decrement(ref _currentItems);
                            OnFlushObjectReceived?.Invoke(@event, EventArgs.Empty);
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
            catch (Exception e)
            {
                options.DiagnosticLogger?.LogFatal("Exception in the background worker.", e);
                throw;
            }
            finally
            {
                queuedEventSemaphore.Dispose();
            }
        }

        public async Task FlushAsync(TimeSpan timeout)
        {
            if (_disposed)
            {
                _options.DiagnosticLogger?.LogDebug("Worker disposed. Nothing to flush.");
                return;
            }

            if (_queue.Count == 0)
            {
                _options.DiagnosticLogger?.LogDebug("No events to flush.");
                return;
            }

            // Start timer from here.
            var timeoutSource = new CancellationTokenSource();
            timeoutSource.CancelAfter(timeout);
            var flushSuccessSource = new CancellationTokenSource();

            var timeoutWithShutdown = CancellationTokenSource.CreateLinkedTokenSource(
                timeoutSource.Token,
                _shutdownSource.Token,
                flushSuccessSource.Token);

            var counter = 0;
            var depth = int.MaxValue;

            void EventFlushedCallback(object objProcessed, EventArgs _)
            {
                // ReSharper disable once AccessToModifiedClosure
                if (Interlocked.Increment(ref counter) >= depth)
                {
                    try
                    {
                        _options.DiagnosticLogger?.LogDebug("Signaling flush completed.");
                        // ReSharper disable once AccessToDisposedClosure
                        flushSuccessSource.Cancel();
                    }
                    catch // Timeout or Shutdown might have been called so this token was disposed.
                    {
                    } // Flush will release when timeout is hit.
                }
            }

            OnFlushObjectReceived += EventFlushedCallback; // Started counting events
            try
            {
                var trackedDepth = _queue.Count;
                if (trackedDepth == 0) // now we're subscribed and counting, make sure it's not already empty.
                {
                    return;
                }

                Interlocked.Exchange(ref depth, trackedDepth);
                _options.DiagnosticLogger?.LogDebug("Tracking depth: {0}.", trackedDepth);

                if (counter >= depth) // When the worker finished flushing before we set the depth
                {
                    return;
                }

                // Await until event is flushed or one of the tokens triggers
                await Task.Delay(timeout, timeoutWithShutdown.Token).ConfigureAwait(false);
                _options.DiagnosticLogger?.LogDebug("Timeout when trying to flush queue.");
            }
            catch (OperationCanceledException)
            {
                _options.DiagnosticLogger?.LogDebug(flushSuccessSource.IsCancellationRequested
                    ? "Successfully flushed all events up to call to FlushAsync."
                    : "Timeout when trying to flush queue.");
            }
            finally
            {
                OnFlushObjectReceived -= EventFlushedCallback;
                timeoutWithShutdown.Dispose();
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
                _shutdownSource.Cancel();

                // If there's anything in the queue, it'll keep running until 'shutdownTimeout' is reached
                // If the queue is empty it will quit immediately
                WorkerTask.Wait(_options.ShutdownTimeout);
            }
            catch (OperationCanceledException)
            {
                 _options.DiagnosticLogger?.LogDebug("Stopping the background worker due to a cancellation");
            }
            catch (Exception exception)
            {
                _options.DiagnosticLogger?.LogError("Stopping the background worker threw an exception.", exception);
            }

            if (_queue.Count > 0)
            {
                _options.DiagnosticLogger?.LogWarning("Worker stopped while {0} were still in the queue.",
                    _queue.Count);
            }
        }
    }
}

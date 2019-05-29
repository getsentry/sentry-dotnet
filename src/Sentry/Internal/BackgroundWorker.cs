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
        private readonly ConcurrentQueue<object> _queue;
        private readonly CancellationTokenSource _shutdownSource;
        private readonly SemaphoreSlim _inSemaphore;
        private readonly SemaphoreSlim _outSemaphore;
        private volatile bool _disposed;

        private event EventHandler OnFlushObjectReceived;

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
            ConcurrentQueue<object> queue = null)
        {
            Debug.Assert(transport != null);
            Debug.Assert(options != null);

            _inSemaphore = new SemaphoreSlim(options.MaxQueueItems, options.MaxQueueItems);
            _outSemaphore = new SemaphoreSlim(0, options.MaxQueueItems);
            _options = options;

            _shutdownSource = cancellationTokenSource ?? new CancellationTokenSource();
            _queue = queue ?? new ConcurrentQueue<object>();

            WorkerTask = Task.Run(
                async () => await WorkerAsync(
                    _queue,
                    _options,
                    transport,
                    _inSemaphore,
                    _outSemaphore,
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

            var acquired = _inSemaphore.Wait(TimeSpan.Zero);
            if (acquired)
            {
                _queue.Enqueue(@event);
                _outSemaphore.Release();
            }
            return acquired;
        }

        private async Task WorkerAsync(
           ConcurrentQueue<object> queue,
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

                    if (queue.TryPeek(out var obj)) // Work with the event while it's in the queue
                    {
                        try
                        {
                            if (obj is SentryEvent @event)
                            {
                                var task = transport.CaptureEventAsync(@event, shutdownTimeout.Token);
                                options.DiagnosticLogger?.LogDebug("Event {0} in-flight to Sentry. #{1} in queue.", @event.EventId, queue.Count);
                                await task.ConfigureAwait(false);
                            }
                            else
                            {
                                OnFlushObjectReceived?.Invoke(obj, EventArgs.Empty);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            options.DiagnosticLogger?.LogInfo("Shutdown token triggered. Time to exit. #{0} in queue.", queue.Count);
                            return;
                        }
                        catch (Exception exception)
                        {
                            options.DiagnosticLogger?.LogError("Error while processing event {1}: {0}. #{2} in queue.", exception, (obj as SentryEvent)?.EventId, queue.Count);
                        }
                        finally
                        {
                            queue.TryDequeue(out _);
                            inSemaphore.Release();
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

        public async Task FlushAsync(TimeSpan timeout)
        {
            if (_disposed)
            {
                _options.DiagnosticLogger?.LogDebug("Worker disposed. Nothing to flush.");
                return;
            }

            if (!_queue.TryPeek(out _))
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

            var objToWait = new object();

            void EventFlushedCallback(object objProcessed, EventArgs _)
            {
                if (ReferenceEquals(objToWait, objProcessed))
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

            OnFlushObjectReceived += EventFlushedCallback;
            try
            {
                // Wait for a slot in the queue or one of the tokens triggers
                var acquired = await _inSemaphore.WaitAsync(timeout, timeoutWithShutdown.Token).ConfigureAwait(false);
                if (acquired)
                {
                    _queue.Enqueue(objToWait);
                    _outSemaphore.Release();
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

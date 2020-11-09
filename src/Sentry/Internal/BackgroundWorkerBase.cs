using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal
{
    internal abstract class BackgroundWorkerBase : IBackgroundWorker, IDisposable
    {
        private readonly int _maxItems;
        private readonly SemaphoreSlim _queuedEnvelopeSemaphore;

        private readonly ConcurrentQueue<Envelope> _queue = new ConcurrentQueue<Envelope>();
        private readonly CancellationTokenSource _shutdownSource = new CancellationTokenSource();

        private volatile bool _disposed;
        private int _currentItems;

        private event EventHandler? OnFlushObjectReceived;

        protected ITransport Transport { get; }

        protected SentryOptions Options { get; }

        public int QueueLength => _queue.Count;

        internal Task WorkerTask { get; }

        protected BackgroundWorkerBase(ITransport transport, SentryOptions options)
        {
            Transport = transport;
            Options = options;

            _maxItems = options.MaxQueueItems;
            _queuedEnvelopeSemaphore = new SemaphoreSlim(0, _maxItems);

            WorkerTask = Task.Run(async () => await WorkerAsync().ConfigureAwait(false));
        }

        protected abstract ValueTask ProcessEnvelopeAsync(
            Envelope envelope,
            CancellationToken cancellationToken = default
        );

        private async ValueTask WorkerAsync()
        {
            var cancellation = _shutdownSource.Token;

            using var shutdownTimeout = new CancellationTokenSource();
            var shutdownRequested = false;

            try
            {
                while (!shutdownTimeout.IsCancellationRequested)
                {
                    // If the cancellation was signaled,
                    // set the latest we can keep reading off of the queue (while there's still stuff to read)
                    // No longer synchronized with queuedEnvelopeSemaphore (Enqueue will throw object disposed),
                    // run until the end of the queue or shutdownTimeout
                    if (!shutdownRequested)
                    {
                        try
                        {
                            await _queuedEnvelopeSemaphore.WaitAsync(cancellation).ConfigureAwait(false);
                        }
                        // Cancellation requested, scheduled shutdown but continue in case there are more items
                        catch (OperationCanceledException)
                        {
                            if (Options.ShutdownTimeout == TimeSpan.Zero)
                            {
                                Options.DiagnosticLogger?.LogDebug(
                                    "Exiting immediately due to 0 shutdown timeout. #{0} in queue.",
                                    _queue.Count
                                );

                                return;
                            }
                            else
                            {
                                Options.DiagnosticLogger?.LogDebug(
                                    "Shutdown scheduled. Stopping by: {0}. #{1} in queue.",
                                    Options.ShutdownTimeout,
                                    _queue.Count
                                );

                                shutdownTimeout.CancelAfter(Options.ShutdownTimeout);
                            }

                            shutdownRequested = true;
                        }
                    }

                    if (_queue.TryPeek(out var envelope)) // Work with the envelope while it's in the queue
                    {
                        try
                        {
                            try
                            {
                                var task = ProcessEnvelopeAsync(envelope, shutdownTimeout.Token);

                                Options.DiagnosticLogger?.LogDebug(
                                    "Envelope {0} in-flight to Sentry. #{1} in queue.",
                                    envelope.TryGetEventId(),
                                    _queue.Count
                                );

                                await task.ConfigureAwait(false);
                            }
                            finally
                            {
                                envelope.Dispose();
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            Options.DiagnosticLogger?.LogInfo(
                                "Shutdown token triggered. Time to exit. #{0} in queue.",
                                _queue.Count
                            );

                            return;
                        }
                        catch (Exception exception)
                        {
                            Options.DiagnosticLogger?.LogError(
                                "Error while processing event {0}. #{1} in queue.",
                                exception,
                                envelope.TryGetEventId(),
                                _queue.Count
                            );
                        }
                        finally
                        {
                            _ = _queue.TryDequeue(out _);
                            _ = Interlocked.Decrement(ref _currentItems);
                            OnFlushObjectReceived?.Invoke(envelope, EventArgs.Empty);
                        }
                    }
                    else
                    {
                        Debug.Assert(shutdownRequested);
                        Options.DiagnosticLogger?.LogInfo("Exiting the worker with an empty queue.");

                        // Empty queue. Exit.
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Options.DiagnosticLogger?.LogFatal("Exception in the background worker.", e);
                throw;
            }
        }

        public void Shutdown() => _shutdownSource.Cancel();

        public async ValueTask FlushAsync(TimeSpan timeout)
        {
            if (_disposed)
            {
                Options.DiagnosticLogger?.LogDebug("Worker disposed. Nothing to flush.");
                return;
            }

            if (_queue.Count == 0)
            {
                Options.DiagnosticLogger?.LogDebug("No events to flush.");
                return;
            }

            // Start timer from here.
            using var timeoutSource = new CancellationTokenSource(timeout);
            using var flushSuccessSource = new CancellationTokenSource();

            using var timeoutWithShutdown = CancellationTokenSource.CreateLinkedTokenSource(
                timeoutSource.Token,
                _shutdownSource.Token,
                flushSuccessSource.Token
            );

            var counter = 0;
            var depth = int.MaxValue;

            void EventFlushedCallback(object objProcessed, EventArgs _)
            {
                // ReSharper disable once AccessToModifiedClosure
                if (Interlocked.Increment(ref counter) >= depth)
                {
                    try
                    {
                        Options.DiagnosticLogger?.LogDebug("Signaling flush completed.");
                        // ReSharper disable once AccessToDisposedClosure
                        flushSuccessSource.Cancel();
                    }
                    catch // Timeout or Shutdown might have been called so this token was disposed.
                    {
                        // Flush will release when timeout is hit.
                    }
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

                _ = Interlocked.Exchange(ref depth, trackedDepth);
                Options.DiagnosticLogger?.LogDebug("Tracking depth: {0}.", trackedDepth);

                if (counter >= depth) // When the worker finished flushing before we set the depth
                {
                    return;
                }

                // Await until event is flushed or one of the tokens triggers
                await Task.Delay(timeout, timeoutWithShutdown.Token).ConfigureAwait(false);
                Options.DiagnosticLogger?.LogDebug("Timeout when trying to flush queue.");
            }
            catch (OperationCanceledException)
            {
                Options.DiagnosticLogger?.LogDebug(flushSuccessSource.IsCancellationRequested
                    ? "Successfully flushed all events up to call to FlushAsync."
                    : "Timeout when trying to flush queue."
                );
            }
            finally
            {
                OnFlushObjectReceived -= EventFlushedCallback;
            }
        }

        public bool EnqueueEnvelope(Envelope envelope)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(BackgroundWorkerBase));
            }

            if (Interlocked.Increment(ref _currentItems) > _maxItems)
            {
                _ = Interlocked.Decrement(ref _currentItems);
                return false;
            }

            _queue.Enqueue(envelope);
            _ = _queuedEnvelopeSemaphore.Release();

            return true;
        }

        /// <summary>
        /// Stops the background worker and waits for it to empty the queue until 'shutdownTimeout' is reached
        /// </summary>
        /// <inheritdoc />
        public void Dispose()
        {
            Options.DiagnosticLogger?.LogDebug("Disposing BackgroundWorker.");

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
                WorkerTask.Wait(Options.ShutdownTimeout);
            }
            catch (OperationCanceledException)
            {
                Options.DiagnosticLogger?.LogDebug("Stopping the background worker due to a cancellation");
            }
            catch (Exception exception)
            {
                Options.DiagnosticLogger?.LogError("Stopping the background worker threw an exception.", exception);
            }
            finally
            {
                if (_queue.Count > 0)
                {
                    Options.DiagnosticLogger?.LogWarning(
                        "Worker stopped while {0} were still in the queue.",
                        _queue.Count
                    );
                }

                _queuedEnvelopeSemaphore.Dispose();
                _shutdownSource.Dispose();
            }
        }
    }
}

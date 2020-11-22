using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal
{
    internal class BackgroundWorker : IBackgroundWorker, IDisposable
    {
        private readonly ITransport _transport;
        private readonly SentryOptions _options;
        private readonly ConcurrentQueue<Envelope> _queue;
        private readonly int _maxItems;
        private readonly CancellationTokenSource _shutdownSource;
        private readonly SemaphoreSlim _queuedEnvelopeSemaphore;

        private volatile bool _disposed;
        private int _currentItems;

        private event EventHandler? OnFlushObjectReceived;

        internal Task WorkerTask { get; }

        public int QueuedItems => _queue.Count;

        public BackgroundWorker(
            ITransport transport,
            SentryOptions options)
            : this(transport, options, null)
        {
        }

        internal BackgroundWorker(
            ITransport transport,
            SentryOptions options,
            CancellationTokenSource? shutdownSource = null,
            ConcurrentQueue<Envelope>? queue = null)
        {
            _transport = transport;
            _options = options;
            _queue = queue ?? new ConcurrentQueue<Envelope>();
            _maxItems = options.MaxQueueItems;
            _shutdownSource = shutdownSource ?? new CancellationTokenSource();
            _queuedEnvelopeSemaphore = new SemaphoreSlim(0, _maxItems);

            WorkerTask = Task.Run(async () => await WorkerAsync().ConfigureAwait(false));
        }

        public bool EnqueueEnvelope(Envelope envelope)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(BackgroundWorker));
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

        private async Task WorkerAsync()
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
                            if (_options.ShutdownTimeout == TimeSpan.Zero)
                            {
                                _options.DiagnosticLogger?.LogDebug(
                                    "Exiting immediately due to 0 shutdown timeout. #{0} in queue.",
                                    _queue.Count
                                );

                                return;
                            }
                            else
                            {
                                _options.DiagnosticLogger?.LogDebug(
                                    "Shutdown scheduled. Stopping by: {0}. #{1} in queue.",
                                    _options.ShutdownTimeout,
                                    _queue.Count
                                );

                                shutdownTimeout.CancelAfter(_options.ShutdownTimeout);
                            }

                            shutdownRequested = true;
                        }
                    }

                    if (_queue.TryPeek(out var envelope)) // Work with the envelope while it's in the queue
                    {
                        try
                        {
                            // Dispose inside try/catch
                            using var _ = envelope;

                            var task = _transport.SendEnvelopeAsync(envelope, shutdownTimeout.Token);

                            _options.DiagnosticLogger?.LogDebug(
                                "Envelope {0} handed off to transport. #{1} in queue.",
                                envelope.TryGetEventId(),
                                _queue.Count
                            );

                            await task.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            _options.DiagnosticLogger?.LogInfo(
                                "Shutdown token triggered. Time to exit. #{0} in queue.",
                                _queue.Count
                            );

                            return;
                        }
                        catch (Exception exception)
                        {
                            _options.DiagnosticLogger?.LogError(
                                "Error while processing event {1}: {0}. #{2} in queue.",
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
                        _options.DiagnosticLogger?.LogInfo("Exiting the worker with an empty queue.");

                        // Empty queue. Exit.
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogFatal("Exception in the background worker.", e);
                throw;
            }
            finally
            {
                _queuedEnvelopeSemaphore.Dispose();
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
                flushSuccessSource.Token
            );

            var counter = 0;
            var depth = int.MaxValue;

            void EventFlushedCallback(object? _, EventArgs __)
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
                    : "Timeout when trying to flush queue."
                );
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
                _ = WorkerTask.Wait(_options.ShutdownTimeout);

                // Dispose the transport if needed
                (_transport as IDisposable)?.Dispose();
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
                _options.DiagnosticLogger?.LogWarning(
                    "Worker stopped while {0} were still in the queue.",
                    _queue.Count
                );
            }
        }
    }
}

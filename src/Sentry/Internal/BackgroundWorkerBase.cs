using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal
{
    internal abstract class BackgroundWorkerBase : IBackgroundWorker, IDisposable
    {
        private readonly ITransport _transport;
        protected SentryOptions Options { get; }

        private readonly CancellationTokenSource _shutdownSource = new CancellationTokenSource();
        private readonly int _maxItems;
        private readonly object _writeLock = new object();
        private readonly SemaphoreSlim _readLock;
        private readonly Task _workerTask;

        private volatile bool _isDisposed;

        private event EventHandler<Envelope>? EnvelopeProcessed;

        public int QueueLength => GetQueueLength();

        public TaskStatus Status => _workerTask.Status;

        protected BackgroundWorkerBase(ITransport transport, SentryOptions options)
        {
            _transport = transport;
            Options = options;

            _maxItems = options.MaxQueueItems;
            _readLock = new SemaphoreSlim(QueueLength, _maxItems);
            _workerTask = Task.Run(async () => await WorkerAsync().ConfigureAwait(false));
        }

        protected abstract int GetQueueLength();

        protected abstract ValueTask<Envelope?> TryGetNextAsync(CancellationToken cancellationToken = default);

        protected abstract void AddToQueue(Envelope envelope);

        protected abstract void RemoveFromQueue(Envelope envelope);

        private async ValueTask WorkerAsync()
        {
            // Create a delayed shutdown source that will trigger after a configurable
            // timeout has passed since the actual shutdown.
            using var delayedShutdownSource = new CancellationTokenSource();
            using var registrationLink = _shutdownSource.Token.Register(() =>
            {
                if (Options.ShutdownTimeout > TimeSpan.Zero)
                {
                    Options.DiagnosticLogger?.LogDebug(
                        "Shutdown scheduled. Stopping after: {0}. #{1} in queue.",
                        Options.ShutdownTimeout,
                        GetQueueLength()
                    );

                    // ReSharper disable once AccessToDisposedClosure
                    delayedShutdownSource.CancelAfter(Options.ShutdownTimeout);
                }
                else
                {
                    Options.DiagnosticLogger?.LogDebug(
                        "Exiting immediately due to 0 shutdown timeout. #{0} in queue.",
                        GetQueueLength()
                    );

                    // ReSharper disable once AccessToDisposedClosure
                    delayedShutdownSource.Cancel();
                }
            });

            var shutdownToken = _shutdownSource.Token;
            var delayedShutdownToken = delayedShutdownSource.Token;

            try
            {
                while (!delayedShutdownToken.IsCancellationRequested)
                {
                    // Wait for semaphore to avoid tight loop.
                    // Semaphore is signaled when a new item is added to the queue.
                    // If shutdown has been requested, this is pass-through until
                    // the queue has been emptied or timeout has been reached.
                    if (!shutdownToken.IsCancellationRequested)
                    {
                        try
                        {
                            await _readLock.WaitAsync(shutdownToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            // Initial shutdown triggered, allow the execution to continue.
                        }
                    }

                    // Get next envelope in queue. This may be null only in the event of shutdown.
                    if (await TryGetNextAsync(delayedShutdownToken).ConfigureAwait(false) is { } envelope)
                    {
                        try
                        {
                            // Dispose envelope inside try/catch
                            using var _ = envelope;

                            Options.DiagnosticLogger?.LogDebug(
                                "Sending envelope {0} to Sentry. #{1} in queue.",
                                envelope.TryGetEventId(),
                                GetQueueLength()
                            );

                            await _transport.SendEnvelopeAsync(envelope, delayedShutdownToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Options.DiagnosticLogger?.LogError(
                                "Error while processing event {0}. #{1} in queue.",
                                ex,
                                envelope.TryGetEventId(),
                                GetQueueLength()
                            );
                        }
                        finally
                        {
                            RemoveFromQueue(envelope);
                            EnvelopeProcessed?.Invoke(this, envelope);
                        }
                    }
                    // If queue is empty and shutdown has been requested,
                    // break early without waiting for the full shutdown timeout.
                    else if (shutdownToken.IsCancellationRequested)
                    {
                        Options.DiagnosticLogger?.LogInfo("Exiting the worker with an empty queue.");
                        break;
                    }
                    else
                    {
                        Debug.Fail(
                            "Invalid state: semaphore acquired, queue empty, but shutdown not requested."
                        );
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                Options.DiagnosticLogger?.LogInfo(
                    "Background worker has shut down. #{0} in queue.",
                    ex,
                    GetQueueLength()
                );
            }
            catch (Exception ex)
            {
                Options.DiagnosticLogger?.LogFatal("Exception in the background worker.", ex);
                throw;
            }
        }

        public void Shutdown() => _shutdownSource.Cancel();

        public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
            {
                Options.DiagnosticLogger?.LogDebug("Worker disposed. Nothing to flush.");
                return;
            }

            if (GetQueueLength() <= 0)
            {
                Options.DiagnosticLogger?.LogDebug("No events to flush.");
                return;
            }

            // Signal that represents a successfully completed flush
            var flushCompletion = new TaskCompletionSource<object?>(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            // Local cancellation and shutdown linked together
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _shutdownSource.Token
            );

            // Transition task to canceled state if any of the tokens trigger
            using var cancellation = linkedTokenSource.Token.Register(
                () => flushCompletion.TrySetCanceled()
            );

            var counter = 0;
            var depth = int.MaxValue;

            void OnEnvelopeProcessed(object sender, Envelope envelope)
            {
                // ReSharper disable once AccessToModifiedClosure
                if (Interlocked.Increment(ref counter) >= depth)
                {
                    Options.DiagnosticLogger?.LogDebug("Signaling flush completed.");
                    flushCompletion.TrySetResult(null);
                }
            }

            EnvelopeProcessed += OnEnvelopeProcessed; // Started counting events
            try
            {
                var trackedDepth = GetQueueLength();
                if (trackedDepth == 0) // now we're subscribed and counting, make sure it's not already empty.
                {
                    return;
                }

                Interlocked.Exchange(ref depth, trackedDepth);
                Options.DiagnosticLogger?.LogDebug("Tracking depth: {0}.", trackedDepth);

                if (counter >= depth) // When the worker finished flushing before we set the depth
                {
                    return;
                }

                // Await until event is flushed or one of the tokens triggers
                await flushCompletion.Task.ConfigureAwait(false);
                Options.DiagnosticLogger?.LogDebug("Timeout when trying to flush queue.");
            }
            catch (OperationCanceledException)
            {
                Options.DiagnosticLogger?.LogDebug(
                    flushCompletion.Task.IsCompleted && !flushCompletion.Task.IsFaulted
                        ? "Successfully flushed all events up to call to FlushAsync."
                        : "Timeout when trying to flush queue."
                );
            }
            finally
            {
                EnvelopeProcessed -= OnEnvelopeProcessed;
            }
        }

        public async ValueTask FlushAsync(TimeSpan timeout)
        {
            using var timeoutSource = new CancellationTokenSource(timeout);
            await FlushAsync(timeoutSource.Token).ConfigureAwait(false);
        }

        public bool EnqueueEnvelope(Envelope envelope)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(BackgroundWorkerBase));
            }

            lock (_writeLock)
            {
                if (GetQueueLength() >= _maxItems)
                {
                    return false;
                }

                AddToQueue(envelope);

                return true;
            }
        }

        /// <summary>
        /// Stops the background worker and waits for it to empty the queue until 'shutdownTimeout' is reached
        /// </summary>
        /// <inheritdoc />
        public void Dispose()
        {
            Options.DiagnosticLogger?.LogDebug("Disposing BackgroundWorker.");

            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            try
            {
                // Immediately requests the worker to stop
                Shutdown();

                // If there's anything in the queue, it'll keep running until 'shutdownTimeout' is reached
                // If the queue is empty it will quit immediately
                _workerTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                Options.DiagnosticLogger?.LogDebug("Stopping the background worker due to cancellation.");
            }
            catch (Exception ex)
            {
                Options.DiagnosticLogger?.LogError("Stopping the background worker threw an exception.", ex);
            }
            finally
            {
                var leftoverCount = GetQueueLength();
                if (leftoverCount > 0)
                {
                    Options.DiagnosticLogger?.LogWarning(
                        "Worker stopped while {0} were still in the queue.",
                        leftoverCount
                    );
                }

                _shutdownSource.Dispose();
            }
        }
    }
}

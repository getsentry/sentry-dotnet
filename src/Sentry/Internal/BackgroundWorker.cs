using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;
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
                _options.ClientReportRecorder.RecordDiscardedEvents(DiscardReason.QueueOverflow, envelope);
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
                        // Cancellation requested, but continue as there are more items
                        catch (OperationCanceledException) when (_options.ShutdownTimeout == TimeSpan.Zero)
                        {
                            _options.LogDebug("Exiting immediately due to 0 shutdown timeout. #{0} in queue.", _queue.Count);

                            return;
                        }
                        // Cancellation requested, scheduled shutdown
                        catch (OperationCanceledException)
                        {
                            _options.LogDebug(
                                "Shutdown scheduled. Stopping by: {0}. #{1} in queue.",
                                _options.ShutdownTimeout,
                                _queue.Count);

                            shutdownTimeout.CancelAfter(_options.ShutdownTimeout);

                            shutdownRequested = true;
                        }
                    }

                    if (_queue.TryPeek(out var envelope)) // Work with the envelope while it's in the queue
                    {
                        try
                        {
                            // Dispose inside try/catch
                            using var _ = envelope;

                            // Send the envelope
                            var task = _transport.SendEnvelopeAsync(envelope, shutdownTimeout.Token);

                            _options.LogDebug(
                                "Envelope {0} handed off to transport. #{1} in queue.",
                                envelope.TryGetEventId(),
                                _queue.Count);

                            await task.ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            _options.LogInfo(
                                "Shutdown token triggered. Time to exit. #{0} in queue.",
                                _queue.Count);

                            return;
                        }
                        catch (Exception exception)
                        {
                            _options.LogError(
                                "Error while processing envelope (event ID: '{0}'). #{1} in queue.",
                                exception,
                                envelope.TryGetEventId(),
                                _queue.Count);
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
                        _options.LogInfo("Exiting the worker with an empty queue.");

                        // Empty queue. Exit.
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                _options.LogFatal("Exception in the background worker.", e);
                throw;
            }
            finally
            {
                _queuedEnvelopeSemaphore.Dispose();
            }
        }

        public async Task FlushAsync(TimeSpan timeout)
        {
            // Start timer from here.
            using var timeoutSource = new CancellationTokenSource(timeout.AdjustForMaxTimeout());
            using var timeoutWithShutdown = CancellationTokenSource.CreateLinkedTokenSource(
                timeoutSource.Token, _shutdownSource.Token);

            try
            {
                await DoFlushAsync(timeoutWithShutdown.Token).ConfigureAwait(false);
            }
            finally
            {
                // Send a final client report, if there is one.  We do this after flushing the queue, because sending
                // the queued envelopes might encounter situations such as rate limiting, and we want to report those.
                // (Client reports themselves are never rate limited.)
                await SendFinalClientReportAsync(timeoutWithShutdown.Token).ConfigureAwait(false);
            }
        }

        private async Task DoFlushAsync(CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                _options.LogDebug("Worker disposed. Nothing to flush.");
                return;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                _options.LogDebug("Timeout or shutdown already requested. Exiting.");
                return;
            }

            if (_queue.IsEmpty)
            {
                _options.LogDebug("No events to flush.");
                return;
            }

            var completionSource = new TaskCompletionSource<bool>();
            cancellationToken.Register(() => completionSource.SetCanceled());

            var counter = 0;
            var depth = int.MaxValue;

            void EventFlushedCallback(object? _, EventArgs __)
            {
                // ReSharper disable once AccessToModifiedClosure
                if (Interlocked.Increment(ref counter) >= depth)
                {
                    _options.LogDebug("Signaling flush completed.");
                    completionSource.TrySetResult(true);
                }
            }

            OnFlushObjectReceived += EventFlushedCallback; // Started counting events

            var trackedDepth = _queue.Count;
            if (trackedDepth == 0) // now we're subscribed and counting, make sure it's not already empty.
            {
                return;
            }

            _ = Interlocked.Exchange(ref depth, trackedDepth);
            _options.LogDebug("Tracking depth: {0}.", trackedDepth);

            if (counter >= depth) // When the worker finished flushing before we set the depth
            {
                return;
            }

            try
            {
                // Await until event is flushed (or we have cancelled)
                await completionSource.Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // timeout occured. We'll log it below.
            }
            finally
            {
                _options.LogDebug(completionSource.Task.Status == TaskStatus.RanToCompletion
                    ? "Successfully flushed all events up to call to FlushAsync."
                    : "Timeout when trying to flush queue.");

                OnFlushObjectReceived -= EventFlushedCallback;
            }
        }

        private async Task SendFinalClientReportAsync(CancellationToken cancellationToken)
        {
            var clientReport = _options.ClientReportRecorder.GenerateClientReport();
            if (clientReport != null)
            {
                _options.LogDebug("Sending client report after flushing queue.");
                using var envelope = Envelope.FromClientReport(clientReport);

                try
                {
                    await _transport.SendEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _options.LogInfo("Timeout or shutdown while trying to send final client report. Exiting.");
                }
                catch (Exception exception)
                {
                    _options.LogError("Error while sending final client report (event ID: '{0}').",
                        exception, envelope.TryGetEventId());
                }
            }
        }

        /// <summary>
        /// Stops the background worker and waits for it to empty the queue until 'shutdownTimeout' is reached
        /// </summary>
        /// <inheritdoc />
        public void Dispose()
        {
            _options.LogDebug("Disposing BackgroundWorker.");

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
                _options.LogDebug("Stopping the background worker due to a cancellation");
            }
            catch (Exception exception)
            {
                _options.LogError("Stopping the background worker threw an exception.", exception);
            }

            if (!_queue.IsEmpty)
            {
                _options.LogWarning(
                    "Worker stopped while {0} were still in the queue.",
                    _queue.Count);
            }
        }
    }
}

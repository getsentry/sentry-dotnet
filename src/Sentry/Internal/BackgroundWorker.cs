using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Internal.Http;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal;

internal class BackgroundWorker : IBackgroundWorker, IDisposable
{
    private readonly ITransport _transport;
    private readonly SentryOptions _options;
    // private readonly ConcurrentQueue<Envelope> _queue;
    private readonly ConcurrentQueueLite<Envelope> _queue;
    private readonly int _maxItems;
    private readonly CancellationTokenSource _shutdownSource;
    private readonly SemaphoreSlim _queuedEnvelopeSemaphore;

    private volatile bool _disposed;
    private int _currentItems;

    internal event EventHandler? OnFlushObjectReceived;

    internal Task WorkerTask { get; }

    public int QueuedItems => _queue.Count;

    public BackgroundWorker(
        ITransport transport,
        SentryOptions options,
        CancellationTokenSource? shutdownSource = null,
        ConcurrentQueue<Envelope>? queue = null)
    {
        _transport = transport;
        _options = options;
        // _queue = queue ?? new ConcurrentQueue<Envelope>();
        _queue = new ConcurrentQueueLite<Envelope>();
        _maxItems = options.MaxQueueItems;
        _shutdownSource = shutdownSource ?? new CancellationTokenSource();
        _queuedEnvelopeSemaphore = new SemaphoreSlim(0, _maxItems);

        options.LogDebug("Starting BackgroundWorker.");
        WorkerTask = Task.Run(DoWorkAsync);
    }

    /// <inheritdoc />
    public bool EnqueueEnvelope(Envelope envelope) => EnqueueEnvelope(envelope, true);

    /// <summary>
    /// Attempts to enqueue the envelope with the worker.
    /// </summary>
    /// <param name="envelope">The envelope to enqueue.</param>
    /// <param name="process">
    /// Whether to process the next item in the queue after enqueuing this item,
    /// which may or may not be the item being enqueued.  The default is <c>true</c>.
    /// Pass <c>false</c> for testing, when you want to add items to the queue without unblocking the worker.
    /// After items are enqueued, use <see cref="ProcessQueuedItems"/> to unblock the worker to process the items.
    /// Disposing the worker will also unblock the items, which will then be processed until the shutdown timeout
    /// is reached or the queue is emptied.
    /// </param>
    /// <returns>True of queueing was successful. Otherwise, false.</returns>
    public bool EnqueueEnvelope(Envelope envelope, bool process)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(BackgroundWorker));
        }

        var eventId = envelope.TryGetEventId(_options.DiagnosticLogger);
        if (Interlocked.Increment(ref _currentItems) > _maxItems)
        {
            Interlocked.Decrement(ref _currentItems);
            _options.ClientReportRecorder.RecordDiscardedEvents(DiscardReason.QueueOverflow, envelope);
            _options.LogInfo("Discarding envelope {0} because the queue is full.", eventId);
            return false;
        }

        _options.LogDebug("Enqueuing envelope {0}", eventId);
        _queue.Enqueue(envelope);

        if (process)
        {
            _queuedEnvelopeSemaphore.Release();
        }

        return true;
    }

    /// <summary>
    /// Processes the number of queued items specified.
    /// Used only in testing, after calling <see cref="EnqueueEnvelope(Envelope, bool)"/>
    /// when passing <c>process: false</c>.
    /// </summary>
    /// <param name="count">The number of items to process from the queue.</param>
    public void ProcessQueuedItems(int count)
    {
        _queuedEnvelopeSemaphore.Release(count);
    }

    private async Task DoWorkAsync()
    {
        _options.LogDebug("BackgroundWorker Started.");

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
                        await _queuedEnvelopeSemaphore.WaitAsync(_shutdownSource.Token).ConfigureAwait(false);
                    }
                    // Cancellation requested and no timeout allowed, so exit even if there are more items
                    catch (OperationCanceledException) when (_options.ShutdownTimeout == TimeSpan.Zero)
                    {
                        _options.LogDebug("Exiting immediately due to 0 shutdown timeout. {0} items in queue.", _queue.Count);

                        shutdownTimeout.Cancel();

                        return;
                    }
                    // Cancellation requested, scheduled shutdown
                    catch (OperationCanceledException)
                    {
                        _options.LogDebug(
                            "Shutdown scheduled. Stopping by: {0}. {1} items in queue.",
                            _options.ShutdownTimeout,
                            _queue.Count);

                        shutdownTimeout.CancelAfterSafe(_options.ShutdownTimeout);

                        shutdownRequested = true;
                    }
                }

                // Work with the envelope while it's in the queue
                if (_queue.TryPeek(out var envelope))
                {
                    var eventId = envelope.TryGetEventId(_options.DiagnosticLogger);
                    try
                    {
                        // Dispose inside try/catch
                        using var _ = envelope;

                        // Send the envelope
                        var task = _transport.SendEnvelopeAsync(envelope, shutdownTimeout.Token);

                        _options.LogDebug(
                            "Envelope handed off to transport (event ID: '{0}'). {1} items in queue.",
                            eventId,
                            _queue.Count);

                        await task.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (shutdownTimeout.IsCancellationRequested)
                    {
                        _options.LogInfo(
                            "Shutdown token triggered. Time to exit. {0} items in queue.",
                            _queue.Count);

                        return;
                    }
                    catch (Exception exception)
                    {
                        _options.LogError(exception,
                            "Error while processing envelope (event ID: '{0}'). {1} items in queue.",
                            eventId, _queue.Count);
                    }
                    finally
                    {
                        _options.LogDebug("De-queueing event {0}", eventId);
                        _queue.TryDequeue(out _);
                        Interlocked.Decrement(ref _currentItems);
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
            _options.LogFatal(e, "Exception in the background worker.");
            throw;
        }
    }

    public async Task FlushAsync(TimeSpan timeout)
    {
        if (_disposed)
        {
            _options.LogDebug("Worker disposed. Nothing to flush.");
            return;
        }

        // Start timer from here.
        using var timeoutSource = new CancellationTokenSource();
        using var timeoutWithShutdown = CancellationTokenSource.CreateLinkedTokenSource(
            timeoutSource.Token, _shutdownSource.Token);
        timeoutSource.CancelAfterSafe(timeout);

        try
        {
            var stopwatch = Stopwatch.StartNew();
            await DoFlushAsync(timeoutWithShutdown.Token).ConfigureAwait(false);

            // We may not have waited the full timeout amount due to timer precision, so wait a bit longer if needed.
            // See https://github.com/getsentry/sentry-dotnet/issues/1864
            while (!_shutdownSource.IsCancellationRequested &&
                   _queue.Count > 0 &&
                   stopwatch.Elapsed < timeout)
            {
                await Task.Delay(10, CancellationToken.None).ConfigureAwait(false);
            }
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
        if (cancellationToken.IsCancellationRequested)
        {
            _options.LogDebug("Timeout or shutdown already requested. Exiting.");
            return;
        }

        var completionSource = new TaskCompletionSource<bool>();
        cancellationToken.Register(() => completionSource.TrySetCanceled());

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

        try
        {
            // now we're subscribed and counting, make sure it's not already empty.
            var trackedDepth = _queue.Count;
            if (trackedDepth != 0)
            {
                Interlocked.Exchange(ref depth, trackedDepth);
                _options.LogDebug("Tracking depth: {0}.", trackedDepth);

                // Check if the worker didn't finish flushing before we set the depth
                if (counter < depth)
                {
                    // Await until event is flushed (or we have cancelled)
                    await completionSource.Task.ConfigureAwait(false);
                }
            }

            _options.LogDebug("Successfully flushed all events up to call to FlushAsync.");

            if (_transport is CachingTransport cachingTransport && !cancellationToken.IsCancellationRequested)
            {
                _options.LogDebug("Flushing caching transport with remaining flush time.");
                await cachingTransport.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            _options.LogDebug("Timeout when trying to flush queue.");
        }
        finally
        {
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
                _options.LogError(exception, "Error while sending final client report (event ID: '{0}').", envelope.TryGetEventId(_options.DiagnosticLogger));
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
            _options.LogDebug("Already disposed BackgroundWorker.");
            return;
        }

        _disposed = true;

        try
        {
            // Requests the worker to stop.
            // This will cause the current (or next) call to _queuedEnvelopeSemaphore.WaitAsync to throw
            // an OperationCanceledException in DoWorkAsync, which will subsequently stop the worker at the
            // appropriate time.
            _shutdownSource.Cancel();

            // Now wait for the worker stop.
            // This will wait until either the queue is empty, or the shutdown timeout is reached.
            // NOTE: While non-intuitive, do not pass a timeout or cancellation token here.  We are waiting for
            // the _continuation_ of the method, not its _execution_.  If we stop waiting prematurely, we may
            // leave the task in the "WaitingForActivation" state, which has resulted in some flaky tests and
            // may cause unexpected behavior in client applications.
            WorkerTask.Wait();
        }
        catch (OperationCanceledException)
        {
            _options.LogDebug("Stopping the background worker due to a cancellation.");
        }
        catch (Exception exception)
        {
            _options.LogError(exception, "Stopping the background worker threw an exception.");
        }
        finally
        {
            if (!_queue.IsEmpty)
            {
                _options.LogWarning("Worker stopped while {0} were still in the queue.", _queue.Count);
            }

            _queuedEnvelopeSemaphore.Dispose();
            _shutdownSource.Dispose();

            // Dispose the transport if needed
            (_transport as IDisposable)?.Dispose();
        }
    }
}

/// <summary>
/// This class is purely for testing purposes. It's been hacked together in a short amount of time. Performance is no
/// doubt terrible and it should in no way be used in production code. It does confirm we have a memory issue with the
/// <see cref="ConcurrentQueue{T}"/> class however. See https://github.com/getsentry/sentry-dotnet/issues/2516
/// </summary>
internal class ConcurrentQueueLite<T>
{
    private readonly List<T> _queue = new();
    private int _listCounter = 0;

    public void Enqueue(T item)
    {
        lock (_queue)
        {
            _queue.Add(item);
            _listCounter++;
        }
    }
    public bool TryDequeue([NotNullWhen(true)] out T? item)
    {
        item = default;
        lock (_queue)
        {
            if (_listCounter > 0)
            {
                item = _queue[0];
                _queue.RemoveAt(0);
                _listCounter--;
            }
        }
        return item != null;
    }

    public int Count => _listCounter;

    public bool IsEmpty => _listCounter == 0;

    public bool TryPeek([NotNullWhen(true)] out T? item)
    {
        item = default;
        lock (_queue)
        {
            if (_listCounter > 0)
            {
                item = _queue[0];
            }
        }
        return item != null;
    }
}

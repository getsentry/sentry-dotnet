using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Http;

/// <summary>
/// A transport that caches envelopes to disk and sends them in the background.
/// </summary>
/// <remarks>
/// Note although this class has a <see cref="CachingTransport.DisposeAsync"/>
/// method, it doesn't implement <see cref="IAsyncDisposable"/> as this caused
/// a dependency issue with Log4Net in some situations.
///
/// See https://github.com/getsentry/sentry-dotnet/issues/3178
/// </remarks>
internal class CachingTransport : ITransport, IDisposable
{
    private const string EnvelopeFileExt = "envelope";
    private const string ProcessingFolder = "__processing";

    private readonly ITransport _innerTransport;
    private readonly SentryOptions _options;
    private readonly bool _failStorage;
    private readonly string _isolatedCacheDirectoryPath;
    private readonly int _keepCount;

    private long _networkIsUnavailable = 0;
    internal bool NetworkIsUnavailable => Interlocked.Read(ref _networkIsUnavailable) == 1;

    // When a file is getting processed, it's moved to a child directory
    // to avoid getting picked up by other threads.
    private readonly string _processingDirectoryPath;

    // Signal that tells the worker whether there's work it can do.
    // Pre-released because the directory might already have files from previous sessions.
    private readonly Signal _workerSignal = new(true);

    // Signal that ensures only one thread can process items from the cache at a time
    private readonly Signal _processingSignal = new(true);

    // Lock to synchronize file system operations inside the cache directory.
    // It's required because there are multiple threads that may attempt to both read
    // and write from/to the cache directory.
    // Lock usage is minimized by moving files that are being processed to a special directory
    // where collisions are not expected.
    private readonly Lock _cacheDirectoryLock = new();

    private readonly CancellationTokenSource _workerCts = new();
    private Task _worker = null!;

    private ManualResetEventSlim? _initCacheResetEvent = new();
    private ManualResetEventSlim? _preInitCacheResetEvent = new();

    // Inner transport exposed internally primarily for testing
    internal ITransport InnerTransport => _innerTransport;

    private readonly IFileSystem _fileSystem;

    public static CachingTransport Create(ITransport innerTransport, SentryOptions options,
        bool startWorker = true,
        bool failStorage = false)
    {
        var transport = new CachingTransport(innerTransport, options, failStorage);
        transport.Initialize(startWorker);
        return transport;
    }

    private CachingTransport(ITransport innerTransport, SentryOptions options, bool failStorage)
    {
        _innerTransport = innerTransport;
        _options = options;
        _failStorage = failStorage; // For testing
        _fileSystem = options.FileSystem;

        _keepCount = _options.MaxCacheItems >= 1
            ? _options.MaxCacheItems - 1
            : 0; // just in case MaxCacheItems is set to an invalid value somehow (shouldn't happen)

        _isolatedCacheDirectoryPath =
            options.TryGetProcessSpecificCacheDirectoryPath() ??
            throw new InvalidOperationException("Cache directory or DSN is not set.");

        _processingDirectoryPath = Path.Combine(_isolatedCacheDirectoryPath, ProcessingFolder);
    }

    private void Initialize(bool startWorker)
    {
        // Restore any abandoned files from a previous session
        MoveUnprocessedFilesBackToCache();

        // Ensure directories exist
        _fileSystem.CreateDirectory(_isolatedCacheDirectoryPath);
        _fileSystem.CreateDirectory(_processingDirectoryPath);

        // Start a worker, if one is needed
        if (startWorker)
        {
            _options.LogDebug("Starting CachingTransport worker.");
            _worker = Task.Run(CachedTransportBackgroundTaskAsync);
        }
        else
        {
            _worker = Task.CompletedTask;
        }

        // Wait for init timeout, if configured.  (Can't do this without a worker.)
        if (startWorker && _options.InitCacheFlushTimeout > TimeSpan.Zero)
        {
            _options.LogDebug("Blocking initialization to flush the cache.");

            try
            {
                // This will complete just before processing starts in the worker.
                // It ensures that we don't start the timeout period prematurely.
                _preInitCacheResetEvent!.Wait(_workerCts.Token);

                // This will complete either when the first round of processing is done,
                // or on timeout, whichever comes first.
                var completed = _initCacheResetEvent!.Wait(_options.InitCacheFlushTimeout, _workerCts.Token);
                if (completed)
                {
                    _options.LogDebug("Completed flushing the cache. Resuming initialization.");
                }
                else
                {
                    _options.LogDebug(
                        $"InitCacheFlushTimeout of {_options.InitCacheFlushTimeout} reached. " +
                        "Resuming initialization. Cache will continue flushing in the background.");
                }
            }
            finally
            {
                // We're done with these.  Dispose them.
                _preInitCacheResetEvent!.Dispose();
                _initCacheResetEvent!.Dispose();

                //Set null to avoid object disposed exceptions on future processing calls.
                _preInitCacheResetEvent = null;
                _initCacheResetEvent = null;
            }
        }
    }

    private async Task CachedTransportBackgroundTaskAsync()
    {
        _options.LogDebug("CachingTransport worker has started.");

        while (!_workerCts.IsCancellationRequested)
        {
            try
            {
                await _workerSignal.WaitAsync(_workerCts.Token).ConfigureAwait(false);
                _options.LogDebug("CachingTransport worker signal triggered.");
                await ProcessCacheAsync(_workerCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_workerCts.IsCancellationRequested)
            {
                // Swallow if IsCancellationRequested as it'll get out of the loop
                break;
            }
            catch (Exception ex)
            {
                _options.LogError(ex, "Exception in CachingTransport worker.");

                try
                {
                    // Wait a bit before retrying
                    await Task.Delay(500, _workerCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break; // Shutting down triggered while waiting
                }
            }
        }
        _options.LogDebug("CachingTransport worker stopped.");
    }

    private void MoveUnprocessedFilesBackToCache()
    {
        // Processing directory may already contain some files left from previous session
        // if the cache was working when the process terminated unexpectedly.
        // Move everything from that directory back to cache directory.

        if (!_fileSystem.DirectoryExists(_processingDirectoryPath))
        {
            // nothing to do
            return;
        }

        foreach (var filePath in _fileSystem.EnumerateFiles(_processingDirectoryPath))
        {
            var destinationPath = Path.Combine(_isolatedCacheDirectoryPath, Path.GetFileName(filePath));
            _options.LogDebug("Moving unprocessed file back to cache: {0} to {1}.", filePath, destinationPath);

            const int maxAttempts = 3;
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    _fileSystem.MoveFile(filePath, destinationPath);
                    break;
                }
                catch (Exception ex)
                {
                    if (!_fileSystem.FileExists(filePath))
                    {
                        _options.LogDebug(
                            "Failed to move unprocessed file back to cache (attempt {0}), " +
                            "but the file no longer exists so it must have been handled by another process: {1}",
                            attempt, filePath);
                        break;
                    }

                    if (attempt < maxAttempts)
                    {
                        _options.LogDebug(
                            "Failed to move unprocessed file back to cache (attempt {0}, retrying.): {1}",
                            attempt, filePath);

                        Thread.Sleep(200); // give a small bit of time before retry
                    }
                    else
                    {
                        _options.LogError(ex,
                            "Failed to move unprocessed file back to cache (attempt {0}, done.): {1}", attempt, filePath);
                    }

                    // note: we do *not* want to re-throw the exception
                }
            }
        }
    }

    private void EnsureFreeSpaceInCache()
    {
        // Trim files, leaving only (X - 1) of the newest ones.
        // X-1 because we need at least 1 empty space for an envelope we're about to add.
        // Example:
        // Limit: 3
        // [f1] [f2] [f3] [f4] [f5]
        //                |-------| <- keep these ones
        // |------------|           <- delete these ones
        var excessCacheFilePaths = GetCacheFilePaths().SkipLast(_keepCount).ToArray();

        foreach (var filePath in excessCacheFilePaths)
        {
            try
            {
                _fileSystem.DeleteFile(filePath);
                _options.LogDebug("Deleted cached file {0}.", filePath);
            }
            catch (FileNotFoundException)
            {
                // File has already been deleted (unexpected but not critical)
                _options.LogWarning(
                    "Cached envelope '{0}' has already been deleted.",
                    filePath);
            }
        }
    }

    private IEnumerable<string> GetCacheFilePaths() =>
        _fileSystem.EnumerateFiles(_isolatedCacheDirectoryPath, $"*.{EnvelopeFileExt}")
            .OrderBy(f => _fileSystem.GetFileCreationTime(f));

    private async Task ProcessCacheAsync(CancellationToken cancellation)
    {
        try
        {
            // Make sure we're the only thread processing items
            await _processingSignal.WaitAsync(cancellation).ConfigureAwait(false);

            // Signal that we can start waiting for _options.InitCacheFlushTimeout
            _preInitCacheResetEvent?.Set();

            // Process the cache
            _options.LogDebug("Flushing cached envelopes.");
            while (await TryPrepareNextCacheFileAsync(cancellation).ConfigureAwait(false) is { } file)
            {
                await InnerProcessCacheAsync(file, cancellation).ConfigureAwait(false);
            }

            // If we reach this point, we've successfully sent an envelope so the network must be available
            if (Interlocked.Exchange(ref _networkIsUnavailable, 0) == 1)
            {
                // Retry envelopes that got stuck in processing while the transport was failing
                MoveUnprocessedFilesBackToCache();
                _workerSignal.Release();
            }

            // Signal that we can continue with initialization, if we're using _options.InitCacheFlushTimeout
            _initCacheResetEvent?.Set();
        }
        finally
        {
            // Release the signal so the next pass or another thread can enter
            _processingSignal.Release();
        }
    }

    private static bool IsNetworkError(Exception exception) =>
        exception switch
        {
            HttpRequestException or WebException or IOException or SocketException => true,
            _ => false
        };

    private async Task InnerProcessCacheAsync(string file, CancellationToken cancellation)
    {
        if (_options.NetworkStatusListener is { Online: false } listener)
        {
            _options.LogDebug("The network is offline. Pausing processing.");
            await listener.WaitForNetworkOnlineAsync(cancellation).ConfigureAwait(false);
            _options.LogDebug("The network is back online. Resuming processing.");
        }

        _options.LogDebug("Reading cached envelope: {0}", file);

        try
        {
            var stream = _fileSystem.OpenFileForReading(file);
#if NETFRAMEWORK || NETSTANDARD2_0
            using (stream)
#else
            await using (stream.ConfigureAwait(false))
#endif
            {
                using (var envelope = await Envelope.DeserializeAsync(stream, cancellation).ConfigureAwait(false))
                {
                    // Don't even try to send it if we are requesting cancellation.
                    cancellation.ThrowIfCancellationRequested();

                    try
                    {
                        _options.LogDebug("Sending cached envelope: {0}",
                            envelope.TryGetEventId(_options.DiagnosticLogger));

                        await _innerTransport.SendEnvelopeAsync(envelope, cancellation).ConfigureAwait(false);
                    }
                    // OperationCancel should not log an error
                    catch (OperationCanceledException ex)
                    {
                        _options.LogDebug("Canceled sending cached envelope: {0}, retrying after a delay.", ex, file);
                        // Let the worker catch, log, wait a bit and retry.
                        throw;
                    }
                    catch (Exception ex) when (IsNetworkError(ex))
                    {
                        Interlocked.Exchange(ref _networkIsUnavailable, 1);
                        _options.LogError(ex, "Failed to send cached envelope: {0}, retrying after a delay.", file);
                        // Let the worker catch, log, wait a bit and retry.
                        throw;
                    }
                    catch (Exception ex) when (ex.Source == "FakeFailingTransport")
                    {
                        // HACK: Deliberately sent from unit tests to avoid deleting the file from processing
                        return;
                    }
                    catch (Exception ex)
                    {
                        _options.ClientReportRecorder.RecordDiscardedEvents(DiscardReason.CacheOverflow, envelope);
                        LogFailureWithDiscard(file, ex);
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            // Log deserialization errors
            LogFailureWithDiscard(file, ex);
        }

        // Envelope & file stream must be disposed prior to reaching this point

        // Delete the envelope file and move on to the next one
        _fileSystem.DeleteFile(file);
    }

    private void LogFailureWithDiscard(string file, Exception ex)
    {
        string? envelopeContents = null;
        try
        {
            if (_fileSystem.FileExists(file))
            {
                envelopeContents = _fileSystem.ReadAllTextFromFile(file);
            }
        }
        // ReSharper disable once EmptyGeneralCatchClause
        catch
        {
        }

        if (envelopeContents == null)
        {
            _options.LogError(ex, "Failed to send cached envelope: {0}, discarding cached envelope.", file);
        }
        else
        {
            _options.LogError(ex, "Failed to send cached envelope: {0}, discarding cached envelope. Envelope contents: {1}", file, envelopeContents);
        }
    }

    // Gets the next cache file and moves it to "processing"
    private async Task<string?> TryPrepareNextCacheFileAsync(CancellationToken cancellationToken = default)
    {
        using var lockClaim = await _cacheDirectoryLock.AcquireAsync(cancellationToken).ConfigureAwait(false);

        var filePath = GetCacheFilePaths().FirstOrDefault();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _options.LogDebug("No cached file to process.");
            return null;
        }

        var targetFilePath = Path.Combine(_processingDirectoryPath, Path.GetFileName(filePath));

        // Move the file to processing.
        // We move with overwrite just in case a file with the same name already exists in the output directory.
        // That should never happen under normal workflows because the filenames have high variance.
        _fileSystem.MoveFile(filePath, targetFilePath, overwrite: true);

        return targetFilePath;
    }

    private async Task StoreToCacheAsync(
        Envelope envelope,
        CancellationToken cancellationToken = default)
    {
        if (_failStorage)
        {
            throw new("Simulated failure writing to storage (for testing).");
        }

        // Envelope file name can be either:
        // 1604679692_2035_b2495755f67e4bb8a75504e5ce91d6c1_17754019.envelope
        // 1604679692_2035__17754019_2035660868.envelope
        // (depending on whether event ID is present or not)
        var envelopeFilePath = Path.Combine(
            _isolatedCacheDirectoryPath,
            $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_" + // timestamp for variance & sorting
            $"{Guid.NewGuid().GetHashCode() % 1_0000}_" + // random 1-4 digits for extra variance
            $"{envelope.TryGetEventId(_options.DiagnosticLogger)}_" + // event ID (may be empty)
            $"{envelope.GetHashCode()}" + // envelope hash code
            $".{EnvelopeFileExt}");

        _options.LogDebug("Storing file {0}.", envelopeFilePath);

        using var lockClaim = await _cacheDirectoryLock.AcquireAsync(cancellationToken).ConfigureAwait(false);

        EnsureFreeSpaceInCache();

        var stream = _fileSystem.CreateFileForWriting(envelopeFilePath);
#if NETFRAMEWORK || NETSTANDARD2_0
        using(stream)
#else
        await using (stream.ConfigureAwait(false))
#endif
        {
            await envelope.SerializeAsync(stream, _options.DiagnosticLogger, cancellationToken).ConfigureAwait(false);
        }

        // Tell the worker that there is work available
        // (file stream MUST BE DISPOSED prior to this)
        _workerSignal.Release();
    }

    // Used locally and in tests
    internal int GetCacheLength() => GetCacheFilePaths().Count();

    // This method asynchronously blocks until the envelope is written to cache, but not until it's sent
    public async Task SendEnvelopeAsync(
        Envelope envelope,
        CancellationToken cancellationToken = default)
    {
        // Client reports should be generated here so they get included in the cached data
        var clientReport = _options.ClientReportRecorder.GenerateClientReport();
        if (clientReport != null)
        {
            envelope = envelope.WithItem(EnvelopeItem.FromClientReport(clientReport));
            _options.LogDebug("Attached client report to envelope {0}.", envelope.TryGetEventId(_options.DiagnosticLogger));
        }

        try
        {
            // Store the envelope in a file without actually sending it anywhere.
            // The envelope will get picked up by the background thread eventually.
            await StoreToCacheAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // On any failure writing to the cache, recover the client report.
            if (clientReport != null)
            {
                _options.ClientReportRecorder.Load(clientReport);
            }

            throw;
        }
    }

    public Task StopWorkerAsync()
    {
        if (_worker.IsCompleted)
        {
            // already stopped
            return Task.CompletedTask;
        }

        // Stop worker and wait until it finishes
        _options.LogDebug("Stopping CachingTransport worker.");
        _workerCts.Cancel();
        return _worker;
    }

    public Task FlushAsync(CancellationToken cancellationToken = default)
    {
        _options.LogDebug("CachingTransport received request to flush the cache.");
        return ProcessCacheAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await StopWorkerAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Don't throw inside dispose
            _options.LogError(ex, "Error stopping worker during dispose.");
        }

        _workerSignal.Dispose();
        _workerCts.Dispose();
        _worker.Dispose();
        _cacheDirectoryLock.Dispose();
        _preInitCacheResetEvent?.Dispose();
        _initCacheResetEvent?.Dispose();

        (_innerTransport as IDisposable)?.Dispose();
    }

    public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
}

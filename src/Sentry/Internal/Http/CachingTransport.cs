using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Http
{
    internal class CachingTransport : ITransport, IAsyncDisposable, IDisposable
    {
        private const string EnvelopeFileExt = "envelope";

        private readonly ITransport _innerTransport;
        private readonly SentryOptions _options;
        private readonly string _isolatedCacheDirectoryPath;
        private readonly int _keepCount;

        // When a file is getting processed, it's moved to a child directory
        // to avoid getting picked up by other threads.
        private readonly string _processingDirectoryPath;

        // Signal that tells the worker whether there's work it can do.
        // Pre-released because the directory might already have files from previous sessions.
        private readonly Signal _workerSignal = new(true);

        // Lock to synchronize file system operations inside the cache directory.
        // It's required because there are multiple threads that may attempt to both read
        // and write from/to the cache directory.
        // Lock usage is minimized by moving files that are being processed to a special directory
        // where collisions are not expected.
        private readonly Lock _cacheDirectoryLock = new();

        private readonly CancellationTokenSource _workerCts = new();
        private readonly Task _worker;

        public CachingTransport(ITransport innerTransport, SentryOptions options)
        {
            _innerTransport = innerTransport;
            _options = options;

            _keepCount = _options.MaxCacheItems >= 1
                ? _options.MaxCacheItems - 1
                : 0; // just in case MaxCacheItems is set to an invalid value somehow (shouldn't happen)

            _isolatedCacheDirectoryPath =
                options.TryGetProcessSpecificCacheDirectoryPath() ??
                throw new InvalidOperationException("Cache directory or DSN is not set.");

            _processingDirectoryPath = Path.Combine(_isolatedCacheDirectoryPath, "__processing");

            _worker = Task.Run(CachedTransportBackgroundTask);
        }

        private async Task CachedTransportBackgroundTask()
        {
            try
            {
                // Processing directory may already contain some files left from previous session
                // if the worker has been terminated unexpectedly.
                // Move everything from that directory back to cache directory.
                if (Directory.Exists(_processingDirectoryPath))
                {
                    foreach (var filePath in Directory.EnumerateFiles(_processingDirectoryPath))
                    {
                        File.Move(
                            filePath,
                            Path.Combine(_isolatedCacheDirectoryPath, Path.GetFileName(filePath))
                        );
                    }
                }

                while (!_workerCts.IsCancellationRequested)
                {
                    try
                    {
                        await _workerSignal.WaitAsync(_workerCts.Token).ConfigureAwait(false);
                        await ProcessCacheAsync(_workerCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw; // Avoid logging an error.
                    }
                    catch (Exception ex)
                    {
                        _options.DiagnosticLogger?.LogError(
                            "Exception in background worker of CachingTransport.",
                            ex
                        );

                        // Wait a bit before retrying
                        await Task.Delay(500, _workerCts.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Worker has been shut down, it's okay
                _options.DiagnosticLogger?.LogDebug("Background worker of CachingTransport has shutdown.");
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
                    File.Delete(filePath);
                    _options.DiagnosticLogger?.LogDebug("Deleted cached file {0}.", filePath);
                }
                catch (FileNotFoundException)
                {
                    // File has already been deleted (unexpected but not critical)
                    _options.DiagnosticLogger?.LogWarning(
                        "Cached envelope '{0}' has already been deleted.",
                        filePath
                    );
                }
            }
        }

        private IEnumerable<string> GetCacheFilePaths()
        {
            try
            {
                return Directory
                    .EnumerateFiles(_isolatedCacheDirectoryPath, $"*.{EnvelopeFileExt}")
                    .OrderBy(f => new FileInfo(f).CreationTimeUtc);
            }
            catch (DirectoryNotFoundException)
            {
                _options.DiagnosticLogger?.LogWarning(
                    "Cache directory is empty."
                );

                return Array.Empty<string>();
            }
        }

        private async Task ProcessCacheAsync(CancellationToken cancellationToken = default)
        {
            _options.DiagnosticLogger?.LogDebug("Flushing cached envelopes.");

            while (await TryPrepareNextCacheFileAsync(cancellationToken).ConfigureAwait(false) is { } envelopeFilePath)
            {
                _options.DiagnosticLogger?.LogDebug(
                    "Reading cached envelope: {0}",
                    envelopeFilePath
                );

                try
                {
#if !NET461 && !NETSTANDARD2_0
                    await
#endif
                        using var envelopeFile = File.OpenRead(envelopeFilePath);
                    using var envelope = await Envelope.DeserializeAsync(envelopeFile, cancellationToken)
                        .ConfigureAwait(false);

                    _options.DiagnosticLogger?.LogDebug(
                        "Sending cached envelope: {0}",
                        envelope.TryGetEventId()
                    );

                    await _innerTransport.SendEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);

                    _options.DiagnosticLogger?.LogDebug(
                        "Successfully sent cached envelope: {0}",
                        envelope.TryGetEventId()
                    );
                }
                catch (Exception ex) when (IsRetryable(ex))
                {
                    _options.DiagnosticLogger?.LogError(
                        "Failed to send cached envelope: {0}, retrying after a delay.",
                        ex,
                        envelopeFilePath
                    );

                    // Let the worker catch, log, wait a bit and retry.
                    throw;
                }
                catch (Exception ex)
                {
                    _options.DiagnosticLogger?.LogError(
                        "Failed to send cached envelope: {0}, discarding cached envelope.",
                        ex,
                        envelopeFilePath
                    );
                }

                // Envelope & file stream must be disposed prior to reaching this point

                // Delete the envelope file and move on to the next one
                File.Delete(envelopeFilePath);
            }
        }

        // Loading an Envelope only reads the headers. The payload is read lazily, so we do Disk -> Network I/O
        // via stream directly instead of loading the whole file in memory. For that reason capturing an envelope
        // from disk could raise an IOException related to Disk I/O.
        // For that reason, we're not retrying IOException, to avoid any disk related exception from retrying.
        private static bool IsRetryable(Exception exception) =>
            exception is OperationCanceledException // Timed-out or Shutdown triggered
            || exception is HttpRequestException // Myriad of HTTP related errors
            || exception is SocketException; // Network related

        // Gets the next cache file and moves it to "processing"
        private async Task<string?> TryPrepareNextCacheFileAsync(
            CancellationToken cancellationToken = default)
        {
            using var lockClaim = await _cacheDirectoryLock.AcquireAsync(cancellationToken).ConfigureAwait(false);

            var filePath = GetCacheFilePaths().FirstOrDefault();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                _options.DiagnosticLogger?.LogDebug("No cached file to process.");
                return null;
            }

            var targetFilePath = Path.Combine(_processingDirectoryPath, Path.GetFileName(filePath));

            // Ensure that the processing directory exists
            Directory.CreateDirectory(_processingDirectoryPath);

            // Move the file to processing.
            // We move with overwrite just in case a file with the same name
            // already exists in the output directory.
            // That should never happen under normal workflows because the filenames
            // have high variance.
#if NETCOREAPP3_0 || NET5_0
            File.Move(filePath, targetFilePath, true);
#else
            File.Copy(filePath, targetFilePath, true);
            File.Delete(filePath);
#endif

            return targetFilePath;
        }

        private async Task StoreToCacheAsync(
            Envelope envelope,
            CancellationToken cancellationToken = default)
        {
            // Envelope file name can be either:
            // 1604679692_2035_b2495755f67e4bb8a75504e5ce91d6c1_17754019.envelope
            // 1604679692_2035__17754019_2035660868.envelope
            // (depending on whether event ID is present or not)
            var envelopeFilePath = Path.Combine(
                _isolatedCacheDirectoryPath,
                $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_" + // timestamp for variance & sorting
                $"{Guid.NewGuid().GetHashCode() % 1_0000}_" + // random 1-4 digits for extra variance
                $"{envelope.TryGetEventId()}_" + // event ID (may be empty)
                $"{envelope.GetHashCode()}" + // envelope hash code
                $".{EnvelopeFileExt}"
            );

            _options.DiagnosticLogger?.LogDebug("Storing file {0}.", envelopeFilePath);

            using var lockClaim = await _cacheDirectoryLock.AcquireAsync(cancellationToken).ConfigureAwait(false);

            EnsureFreeSpaceInCache();

            Directory.CreateDirectory(_isolatedCacheDirectoryPath);

#if !NET461 && !NETSTANDARD2_0
            await
#endif
                using (var stream = File.Create(envelopeFilePath))
            {
                await envelope.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
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
            // Store the envelope in a file without actually sending it anywhere.
            // The envelope will get picked up by the background thread eventually.
            await StoreToCacheAsync(envelope, cancellationToken).ConfigureAwait(false);
        }

        public async Task StopWorkerAsync()
        {
            // Stop worker and wait until it finishes
            _workerCts.Cancel();
            await _worker.ConfigureAwait(false);
        }

        public async Task FlushAsync(CancellationToken cancellationToken = default) =>
            await ProcessCacheAsync(cancellationToken).ConfigureAwait(false);

        public async ValueTask DisposeAsync()
        {
            try
            {
                await StopWorkerAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Don't throw inside dispose
                _options.DiagnosticLogger?.LogError(
                    "Error stopping worker during dispose.",
                    ex
                );
            }

            _workerSignal.Dispose();
            _workerCts.Dispose();
            _worker.Dispose();
            _cacheDirectoryLock.Dispose();

            (_innerTransport as IDisposable)?.Dispose();
        }

        public void Dispose() => DisposeAsync().GetAwaiter().GetResult();
    }
}

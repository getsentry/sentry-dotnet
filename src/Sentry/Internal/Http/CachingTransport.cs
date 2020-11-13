using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Http
{
    internal class CachingTransport : ITransport, IDisposable
    {
        private const string EnvelopeFileExt = "envelope";

        private readonly ITransport _innerTransport;
        private readonly SentryOptions _options;
        private readonly string _cacheDirectoryPath;

        // When a file is getting processed, it's moved to a child directory
        // to avoid getting picked up by other threads.
        private readonly string _processingDirectoryPath;

        // Signal that tells the worker whether there's work it can do.
        // Pre-released because the directory might already have files from previous sessions.
        private readonly Signal _workerSignal = new Signal(true);

        // Lock to synchronize file system operations inside the cache directory.
        // It's required because there are multiple threads that may attempt to both read
        // and write from/to the cache directory.
        // Lock usage is minimized by moving files that are being processed to a special directory
        // where collisions are not expected.
        private readonly Lock _cachingDirectoryLock = new Lock();

        private readonly CancellationTokenSource _workerCts = new CancellationTokenSource();
        private readonly Task _worker;

        public CachingTransport(ITransport innerTransport, SentryOptions options)
        {
            _innerTransport = innerTransport;
            _options = options;

            _cacheDirectoryPath = !string.IsNullOrWhiteSpace(options.CacheDirectoryPath)
                ? _cacheDirectoryPath = options.CacheDirectoryPath
                : throw new InvalidOperationException("Cache directory is not set.");

            _processingDirectoryPath = Path.Combine(_cacheDirectoryPath, "__processing");

            // Processing directory may already contain some files left from previous session
            // if the worker has been terminated unexpectedly.
            // Move everything from that directory back to cache directory.
            if (Directory.Exists(_processingDirectoryPath))
            {
                foreach (var filePath in Directory.EnumerateFiles(_processingDirectoryPath))
                {
                    File.Move(
                        filePath,
                        Path.Combine(_cacheDirectoryPath, Path.GetFileName(filePath))
                    );
                }
            }

            _worker = Task.Run(async () =>
            {
                while (!_workerCts.IsCancellationRequested)
                {
                    try
                    {
                        await _workerSignal.WaitAsync(_workerCts.Token).ConfigureAwait(false);
                        await ProcessCacheAsync(_workerCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Ignore
                    }
                    catch (Exception ex)
                    {
                        _options.DiagnosticLogger?.LogError(
                            "Exception in background worker of CachingTransport.",
                            ex
                        );
                    }
                }
            });
        }

        private IEnumerable<string> GetCacheFilePaths()
        {
            if (!Directory.Exists(_cacheDirectoryPath))
            {
                return Enumerable.Empty<string>();
            }

            return Directory
                .EnumerateFiles(_cacheDirectoryPath, $"*.{EnvelopeFileExt}")
                .OrderBy(f => new FileInfo(f).CreationTimeUtc);
        }

        public int GetCacheLength() => GetCacheFilePaths().Count();

        // Gets the next cache file and moves it to "processing"
        private async ValueTask<string?> TryPrepareNextCacheFileAsync(
            CancellationToken cancellationToken = default)
        {
            using var lockClaim = await _cachingDirectoryLock.ClaimAsync(cancellationToken).ConfigureAwait(false);

            var filePath = GetCacheFilePaths().FirstOrDefault();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return null;
            }

            var targetFilePath = Path.Combine(_processingDirectoryPath, Path.GetFileName(filePath));

            Directory.CreateDirectory(_processingDirectoryPath);
            File.Move(filePath, targetFilePath);

            return targetFilePath;
        }

        private async ValueTask ProcessCacheAsync(CancellationToken cancellationToken = default)
        {
            _options.DiagnosticLogger?.LogDebug("Flushing cached envelopes.");

            while (await TryPrepareNextCacheFileAsync(cancellationToken).ConfigureAwait(false) is { } envelopeFilePath)
            {
                _options.DiagnosticLogger?.LogDebug(
                    "Reading cached envelope: {0}",
                    envelopeFilePath
                );

                var shouldRetry = false;

                using (var envelopeFile = File.OpenRead(envelopeFilePath))
                using (var envelope =
                    await Envelope.DeserializeAsync(envelopeFile, cancellationToken).ConfigureAwait(false))
                {
                    _options.DiagnosticLogger?.LogDebug(
                        "Sending cached envelope: {0}",
                        envelope.TryGetEventId()
                    );

                    try
                    {
                        await _innerTransport.SendEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);

                        _options.DiagnosticLogger?.LogDebug(
                            "Successfully sent cached envelope: {0}",
                            envelope.TryGetEventId()
                        );
                    }
                    catch (IOException ex)
                    {
                        _options.DiagnosticLogger?.LogError(
                            "Transient failure when sending cached envelope: {0}",
                            ex,
                            envelope.TryGetEventId()
                        );

                        shouldRetry = true;
                    }
                    catch (HttpRequestException ex)
                    {
                        _options.DiagnosticLogger?.LogError(
                            "Persistent failure when sending cached envelope: {0}",
                            ex,
                            envelope.TryGetEventId()
                        );
                    }
                }

                if (shouldRetry)
                {
                    // Move the file back to cache directory so it gets processed again in the future
                    File.Move(
                        envelopeFilePath,
                        Path.Combine(_cacheDirectoryPath, Path.GetFileName(envelopeFilePath))
                    );

                    // Avoid processing other files because the order may end up corrupted
                    break;
                }
                else
                {
                    // Delete the envelope file
                    // (envelope & file stream MUST BE DISPOSED prior to this)
                    File.Delete(envelopeFilePath);
                }
            }
        }

        private async ValueTask StoreToCacheAsync(
            Envelope envelope,
            CancellationToken cancellationToken = default)
        {
            using var lockClaim = await _cachingDirectoryLock.ClaimAsync(cancellationToken).ConfigureAwait(false);

            // If over capacity - remove oldest envelope file
            while (
                GetCacheFilePaths().Count() >= _options.MaxQueueItems &&
                GetCacheFilePaths().FirstOrDefault() is { } oldestEnvelopeFilePath)
            {
                File.Delete(oldestEnvelopeFilePath);
            }

            // Envelope file name can be either:
            // 1604679692_b2495755f67e4bb8a75504e5ce91d6c1_17754019.envelope
            // 1604679692__17754019.envelope
            // (depending on whether event ID is present or not)
            var envelopeFilePath = Path.Combine(
                _cacheDirectoryPath,
                $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_" +
                $"{envelope.TryGetEventId()}_" +
                $"{envelope.GetHashCode()}" +
                $".{EnvelopeFileExt}"
            );

            if (!Directory.Exists(_cacheDirectoryPath))
            {
                _options.DiagnosticLogger?.LogDebug(
                    "Provided cache directory does not exist. Creating it."
                );

                Directory.CreateDirectory(_cacheDirectoryPath);
            }

            using (var stream = File.Create(envelopeFilePath))
            {
                await envelope.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
            }

            // Tell the worker that there is work available
            // (file stream MUST BE DISPOSED prior to this)
            _workerSignal.Release();
        }

        // This method asynchronously blocks until the envelope is written to cache, but not until it's sent.
        // Additionally, if the worker is currently in the process of sending an envelope,
        // this method will also block until the worker finishes processing and releases file system lock.
        public async ValueTask SendEnvelopeAsync(
            Envelope envelope,
            CancellationToken cancellationToken = default)
        {
            // Store the envelope in a file without actually sending it anywhere.
            // The envelope will get picked up by the background thread eventually.
            await StoreToCacheAsync(envelope, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask StopWorkerAsync()
        {
            // Stop worker and wait until it finishes
            _workerCts.Cancel();
            await _worker.ConfigureAwait(false);
        }

        public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
        {
            await ProcessCacheAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            StopWorkerAsync().GetAwaiter().GetResult();

            _workerSignal.Dispose();
            _workerCts.Dispose();
            _worker.Dispose();
        }
    }
}

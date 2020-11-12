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

        // Signal that tells the worker whether there's work it can do.
        // Pre-released because the directory might already have files from previous sessions.
        private readonly Signal _workerSignal = new Signal(true);

        // Lock to synchronize file system operations to prevent collisions when writing/reading from
        // multiple threads.
        private readonly Lock _fileSystemLock = new Lock();

        private readonly CancellationTokenSource _workerCts = new CancellationTokenSource();
        private readonly Task _worker;

        public CachingTransport(ITransport innerTransport, SentryOptions options)
        {
            _innerTransport = innerTransport;
            _options = options;

            _cacheDirectoryPath = !string.IsNullOrWhiteSpace(options.CacheDirectoryPath)
                ? _cacheDirectoryPath = options.CacheDirectoryPath
                : throw new InvalidOperationException("Cache directory is not set.");

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

        private async ValueTask ProcessCacheAsync(CancellationToken cancellationToken = default)
        {
            _options.DiagnosticLogger?.LogDebug("Flushing cached envelopes.");

            foreach (var envelopeFilePath in GetCacheFilePaths())
            {
                // We need to lock file system here, because the consumer might attempt
                // to send an envelope, which in turn might attempt to delete an existing file,
                // which in turn may lead to a race condition over file access.
                using var lockClaim = await _fileSystemLock.ClaimAsync(cancellationToken).ConfigureAwait(false);

                _options.DiagnosticLogger?.LogDebug(
                    "Reading cached envelope: {0}",
                    envelopeFilePath
                );

                using (var envelopeFile = File.OpenRead(envelopeFilePath))
                using (var envelope = await Envelope.DeserializeAsync(envelopeFile, cancellationToken).ConfigureAwait(false))
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

                        // Break on transient exceptions without deleting the cache file
                        break;
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

                // Delete the envelope file
                // (envelope & file stream MUST BE DISPOSED prior to this)
                File.Delete(envelopeFilePath);
            }
        }

        private async ValueTask StoreToCacheAsync(
            Envelope envelope,
            CancellationToken cancellationToken = default)
        {
            // We need to lock file system here, because we may delete a file if the cache is full.
            // Additionally, we don't want the worker to start processing a file we haven't finished writing yet.
            using var lockClaim = await _fileSystemLock.ClaimAsync(cancellationToken).ConfigureAwait(false);

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

        public async ValueTask ShutdownAsync()
        {
            // Stop worker and wait until it finishes
            _workerCts.Cancel();
            await _worker.ConfigureAwait(false);
        }

        public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
        {
            await ShutdownAsync().ConfigureAwait(false);
            await ProcessCacheAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            ShutdownAsync().GetAwaiter().GetResult();

            _workerSignal.Dispose();
            _workerCts.Dispose();
            _worker.Dispose();
        }
    }
}

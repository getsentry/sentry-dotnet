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

        private readonly AsyncLock _workerLock = new AsyncLock();
        private readonly CancellationTokenSource _workerCts = new CancellationTokenSource();
        private readonly Task _worker;

        public CachingTransport(ITransport innerTransport, SentryOptions options)
        {
            _innerTransport = innerTransport;
            _options = options;

            _cacheDirectoryPath = !string.IsNullOrWhiteSpace(options.CacheDirectoryPath)
                ? _cacheDirectoryPath = options.CacheDirectoryPath
                : throw new InvalidOperationException("Cache directory is not set.");

            // Pre-release lock if there are existing files
            if (GetEnvelopeFilePaths().Any())
            {
                _workerLock.Release();
            }

            _worker = Task.Run(async () =>
            {
                while (!_workerCts.IsCancellationRequested)
                {
                    try
                    {
                        await _workerLock.WaitAsync(_workerCts.Token).ConfigureAwait(false);
                        await ProcessCacheAsync(_workerCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            });
        }

        private IEnumerable<string> GetEnvelopeFilePaths()
        {
            if (!Directory.Exists(_cacheDirectoryPath))
            {
                return Enumerable.Empty<string>();
            }

            return Directory
                .EnumerateFiles(_cacheDirectoryPath, $"*.{EnvelopeFileExt}")
                .OrderBy(f => new FileInfo(f).CreationTimeUtc);
        }

        private async ValueTask ProcessCacheAsync(CancellationToken cancellationToken = default)
        {
            _options.DiagnosticLogger?.LogDebug("Flushing cached envelopes.");

            foreach (var envelopeFilePath in GetEnvelopeFilePaths())
            {
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
                // (envelope MUST BE DISPOSED prior to this)
                File.Delete(envelopeFilePath);
            }
        }

        private async ValueTask StoreEnvelopeAsync(
            Envelope envelope,
            CancellationToken cancellationToken = default)
        {
            // If over capacity - remove oldest envelope file
            // TODO: probably a good idea to put a lock here to make sure this limit is maintained
            while (GetEnvelopeFilePaths().Count() >= _options.MaxQueueItems)
            {
                if (GetEnvelopeFilePaths().FirstOrDefault() is { } oldestEnvelopeFilePath)
                {
                    File.Delete(oldestEnvelopeFilePath);
                }
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

            using var stream = File.Create(envelopeFilePath);
            await envelope.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
        {
            // Store the envelope in a file and wait until it's picked up by the background thread
            await StoreEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);
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

            _workerLock.Dispose();
            _workerCts.Dispose();
            _worker.Dispose();
        }
    }
}

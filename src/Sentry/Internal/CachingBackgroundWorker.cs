using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal
{
    internal class CachingBackgroundWorker : BackgroundWorkerBase
    {
        private const string EnvelopeFileExt = "envelope";
        private readonly DirectoryInfo _cacheDirectory;

        public CachingBackgroundWorker(ITransport transport, SentryOptions options)
            : base(transport, options)
        {
            _cacheDirectory = !string.IsNullOrWhiteSpace(options.CacheDirectoryPath)
                ? _cacheDirectory = new DirectoryInfo(options.CacheDirectoryPath)
                : throw new InvalidOperationException("Cache directory is not set.");
        }

        private IEnumerable<FileInfo> GetEnvelopeFiles() =>
            _cacheDirectory.Exists
                ? _cacheDirectory.EnumerateFiles($"*.{EnvelopeFileExt}")
                : Enumerable.Empty<FileInfo>();

        private FileInfo? TryGetNextEnvelopeFile() =>
            GetEnvelopeFiles()
                .OrderBy(f => f.CreationTimeUtc)
                .FirstOrDefault();

        private async ValueTask FlushCacheAsync(
            CancellationToken cancellationToken = default)
        {
            Options.DiagnosticLogger?.LogDebug("Flushing cached envelopes.");

            while (TryGetNextEnvelopeFile() is { } envelopeFile)
            {
                Options.DiagnosticLogger?.LogDebug(
                    "Reading cached envelope: {0}",
                    envelopeFile.FullName
                );

                using var envelope = await Envelope.DeserializeAsync(
                    envelopeFile.OpenRead(),
                    cancellationToken
                ).ConfigureAwait(false);

                Options.DiagnosticLogger?.LogDebug(
                    "Sending cached envelope: {0}",
                    envelope.TryGetEventId()
                );

                try
                {
                    await Transport.SendEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);

                    // Delete the cache file in case of success
                    envelopeFile.Delete();

                    Options.DiagnosticLogger?.LogDebug(
                        "Successfully sent cached envelope: {0}",
                        envelope.TryGetEventId()
                    );
                }
                catch (IOException ex)
                {
                    // Don't delete the cache file in case of transient exceptions,
                    // i.e. loss of connection, failure to connect, etc.
                    // Instead break to not violate the order and wait until
                    // the next batch to attempt sending again.
                    Options.DiagnosticLogger?.LogError(
                        "Transient failure when sending cached envelope: {0}",
                        ex,
                        envelope.TryGetEventId()
                    );

                    break;
                }
                catch (Exception ex)
                {
                    // Delete the cache file for all other exceptions that could
                    // indicate a successfully completed, but error response.
                    envelopeFile.Delete();

                    Options.DiagnosticLogger?.Log(
                        SentryLevel.Error,
                        "Persistent failure when sending cached envelope: {0}",
                        ex,
                        envelope.TryGetEventId()
                    );
                }
            }
        }

        private async ValueTask<FileInfo> StoreEnvelopeAsync(
            Envelope envelope,
            CancellationToken cancellationToken = default)
        {
            // If over capacity - remove oldest envelope file
            // TODO: probably a good idea to put a lock here to make sure this limit is maintained
            while (GetEnvelopeFiles().Count() >= 30)
            {
                if (TryGetNextEnvelopeFile() is { } oldestEnvelopeFile)
                {
                    oldestEnvelopeFile.Delete();
                }
            }

            // Envelope file name can be either:
            // 1604679692_b2495755f67e4bb8a75504e5ce91d6c1_17754019.envelope
            // 1604679692__17754019.envelope
            // (depending on whether event ID is present or not)
            var envelopeFile = new FileInfo(
                Path.Combine(
                    _cacheDirectory.FullName,
                    $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_" +
                    $"{envelope.TryGetEventId()}_" +
                    $"{envelope.GetHashCode()}" +
                    $".{EnvelopeFileExt}")
            );

            _cacheDirectory.Create();
            using var stream = envelopeFile.Create();
            await envelope.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);

            return envelopeFile;
        }

        protected override async ValueTask ProcessEnvelopeAsync(
            Envelope envelope,
            CancellationToken cancellationToken = default)
        {
            // Add this envelope to cache
            await StoreEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);

            // Send cached envelopes via transport, which includes all
            // cached envelopes until this point and the current envelope.
            await FlushCacheAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}

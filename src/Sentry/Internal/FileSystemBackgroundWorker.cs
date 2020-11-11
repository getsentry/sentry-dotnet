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
    internal class FileSystemBackgroundWorker : BackgroundWorkerBase
    {
        private const string EnvelopeFileExt = "envelope";
        private readonly string _cacheDirectoryPath;

        public FileSystemBackgroundWorker(ITransport transport, SentryOptions options)
            : base(transport, options)
        {
            _cacheDirectoryPath = !string.IsNullOrWhiteSpace(options.CacheDirectoryPath)
                ? _cacheDirectoryPath = options.CacheDirectoryPath
                : throw new InvalidOperationException("Cache directory is not set.");
        }

        private IEnumerable<string> GetEnvelopeFilePaths() =>
            Directory.Exists(_cacheDirectoryPath)
                ? Directory.EnumerateFiles(_cacheDirectoryPath, $"*.{EnvelopeFileExt}")
                : Enumerable.Empty<string>();

        private async ValueTask<FileInfo> StoreEnvelopeAsync(
            Envelope envelope,
            CancellationToken cancellationToken = default)
        {
            // Envelope file name can be either:
            // 1604679692_b2495755f67e4bb8a75504e5ce91d6c1_17754019.envelope
            // 1604679692__17754019.envelope
            // (depending on whether event ID is present or not)
            var envelopeFile = new FileInfo(
                Path.Combine(
                    _cacheDirectoryPath,
                    $"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}_" +
                    $"{envelope.TryGetEventId()}_" +
                    $"{envelope.GetHashCode()}" +
                    $".{EnvelopeFileExt}")
            );

            if (!Directory.Exists(_cacheDirectoryPath))
            {
                Options.DiagnosticLogger?.LogDebug(
                    "Provided cache directory does not exist. Creating it."
                );

                Directory.CreateDirectory(_cacheDirectoryPath);
            }

            using var stream = envelopeFile.Create();
            await envelope.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);

            return envelopeFile;
        }

        protected override async ValueTask<Envelope?> TryGetNextAsync(CancellationToken cancellationToken = default)
        {
            var nextEnvelopeFilePath = GetEnvelopeFilePaths().FirstOrDefault();
            if (string.IsNullOrWhiteSpace(nextEnvelopeFilePath))
            {
                return null;
            }

            Options.DiagnosticLogger?.LogDebug(
                "Reading cached envelope: {0}",
                nextEnvelopeFilePath
            );

            return await Envelope.DeserializeAsync(
                File.OpenRead(nextEnvelopeFilePath),
                cancellationToken
            ).ConfigureAwait(false);
        }

        protected override int GetQueueLength() => GetEnvelopeFilePaths().Count();

        protected override void AddToQueue(Envelope envelope) =>
            StoreEnvelopeAsync(envelope).GetAwaiter().GetResult();

        protected override void RemoveFromQueue(Envelope envelope)
        {
            throw new NotImplementedException();
        }
    }
}

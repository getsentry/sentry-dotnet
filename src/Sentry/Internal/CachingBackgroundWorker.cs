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

        private IEnumerable<FileInfo> GetCachedEnvelopeFiles()
        {
            if (!_cacheDirectory.Exists)
            {
                return Enumerable.Empty<FileInfo>();
            }

            return _cacheDirectory.EnumerateFiles($"*.{EnvelopeFileExt}");
        }

        private FileInfo? TryGetNextEnvelopeFile() => GetCachedEnvelopeFiles()
            .OrderBy(f => f.CreationTimeUtc)
            .FirstOrDefault();

        private async ValueTask FlushCacheAsync(CancellationToken cancellationToken = default)
        {
            while (TryGetNextEnvelopeFile() is { } envelopeFile)
            {
                Options.DiagnosticLogger?.LogDebug("Reading cached envelope: {0}", envelopeFile.FullName);

                using var envelope = await Envelope.DeserializeAsync(
                    envelopeFile.OpenRead(),
                    cancellationToken
                ).ConfigureAwait(false);

                Options.DiagnosticLogger?.LogDebug("Sending cached envelope: {0}", envelope.TryGetEventId());

                try
                {
                    await Transport.SendEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);

                    // Delete the cache file in case of success
                    envelopeFile.Delete();
                }
                catch (IOException)
                {
                    // Don't delete the cache file in case of transient exceptions,
                    // i.e. loss of connection, failure to connect, etc.
                }
                catch
                {
                    // Delete the cache file for all other exceptions that could
                    // indicate a successfully completed, but error response.
                    envelopeFile.Delete();
                }
            }
        }

        private async ValueTask CacheEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
        {
            if (GetCachedEnvelopeFiles().Count() >= 30)
            {
                return;
            }

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
        }

        protected override async ValueTask ProcessEnvelopeAsync(
            Envelope envelope,
            CancellationToken cancellationToken = default)
        {
            await FlushCacheAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await Transport.SendEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await CacheEnvelopeAsync(envelope, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

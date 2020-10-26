using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Protocol
{
    internal class StreamSerializable : IDisposable, ISerializable
    {
        private readonly Stream _source;

        public StreamSerializable(Stream source) => _source = source;

        /// <inheritdoc />
        public async Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default) =>
            await _source.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);

        /// <inheritdoc />
        public void Dispose() => _source.Dispose();
    }
}

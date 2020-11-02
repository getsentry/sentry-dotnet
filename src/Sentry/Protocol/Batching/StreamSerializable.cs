using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Protocol.Batching
{
    internal class StreamSerializable : ISerializable, IDisposable
    {
        private readonly Stream _source;

        public StreamSerializable(Stream source) => _source = source;

        public async ValueTask SerializeAsync(Stream stream, CancellationToken cancellationToken = default) =>
            await _source.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);

        public void Dispose() => _source.Dispose();
    }
}

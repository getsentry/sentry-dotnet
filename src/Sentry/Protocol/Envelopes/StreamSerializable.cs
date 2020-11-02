using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Protocol.Envelopes
{
    internal class StreamSerializable : ISerializable, IDisposable
    {
        public Stream Source { get; }

        public StreamSerializable(Stream source) => Source = source;

        public async ValueTask SerializeAsync(Stream stream, CancellationToken cancellationToken = default) =>
            await Source.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);

        public void Dispose() => Source.Dispose();
    }
}

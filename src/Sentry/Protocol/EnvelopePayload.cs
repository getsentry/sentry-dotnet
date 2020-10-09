using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Protocol
{
    /// <summary>
    /// Envelope payload.
    /// </summary>
    public class EnvelopePayload : ISerializable, IDisposable
    {
        /// <summary>
        /// Payload data.
        /// </summary>
        public Stream Stream { get; }

        /// <summary>
        /// Initializes an instance of <see cref="EnvelopePayload"/>.
        /// </summary>
        public EnvelopePayload(Stream stream)
        {
            Stream = stream;
        }

        /// <inheritdoc />
        public async Task SerializeAsync(StreamWriter writer, CancellationToken cancellationToken = default)
        {
            await Stream.CopyToAsync(writer.BaseStream, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void Dispose() => Stream.Dispose();
    }
}

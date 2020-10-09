using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Protocol
{
    /// <summary>
    /// Envelope item.
    /// </summary>
    public class EnvelopeItem : ISerializable
    {
        /// <summary>
        /// Headers associated with this item.
        /// </summary>
        public EnvelopeHeaderCollection Headers { get; }

        /// <summary>
        /// Payload associated with this item.
        /// </summary>
        public EnvelopePayload Payload { get; }

        /// <summary>
        /// Initializes an instance of <see cref="EnvelopeItem"/>.
        /// </summary>
        public EnvelopeItem(EnvelopeHeaderCollection headers, EnvelopePayload payload)
        {
            Headers = headers;
            Payload = payload;
        }

        /// <inheritdoc />
        public async Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            await Headers.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
            stream.WriteByte((byte)'\n');
            await Payload.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }
}

using System.Collections.Generic;
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
        public ISerializable Payload { get; }

        /// <summary>
        /// Initializes an instance of <see cref="EnvelopeItem"/>.
        /// </summary>
        public EnvelopeItem(EnvelopeHeaderCollection headers, ISerializable payload)
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

        public static EnvelopeItem FromFile(string filePath)
        {
            var fileStream = File.OpenRead(filePath);
            var payload = new StreamSerializable(fileStream);

            var headers = new EnvelopeHeaderCollection(new Dictionary<string, object>
            {
                ["type"] = "file",
                ["length"] = fileStream.Length
            });

            return new EnvelopeItem(headers, payload);
        }

        public static EnvelopeItem FromEvent(SentryEvent @event)
        {
            // TODO: calculate length ahead of time?
            var headers = new EnvelopeHeaderCollection(new Dictionary<string, object>
            {
                ["type"] = "event"
            });

            return new EnvelopeItem(headers, @event);
        }
    }
}

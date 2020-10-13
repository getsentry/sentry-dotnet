using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Internal;

namespace Sentry.Protocol
{
    /// <summary>
    /// Envelope item.
    /// </summary>
    public class EnvelopeItem : ISerializable
    {
        /// <summary>
        /// Header associated with this item.
        /// </summary>
        public IReadOnlyDictionary<string, object> Header { get; }

        /// <summary>
        /// Payload associated with this item.
        /// </summary>
        public ISerializable Payload { get; }

        /// <summary>
        /// Initializes an instance of <see cref="EnvelopeItem"/>.
        /// </summary>
        public EnvelopeItem(IReadOnlyDictionary<string, object> header, ISerializable payload)
        {
            Header = header;
            Payload = payload;
        }

        /// <inheritdoc />
        public async Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            // Header
            await JsonSerializer.SerializeObjectAsync(Header, stream, cancellationToken).ConfigureAwait(false);
            stream.WriteByte((byte)'\n');

            // Payload
            await Payload.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
        }

        public static EnvelopeItem FromFile(string filePath)
        {
            var fileStream = File.OpenRead(filePath);
            var payload = new StreamSerializable(fileStream);

            var headers = new Dictionary<string, object>
            {
                ["type"] = "attachment",
                ["file_name"] = Path.GetFileName(filePath),
                ["length"] = fileStream.Length
            };

            return new EnvelopeItem(headers, payload);
        }

        public static EnvelopeItem FromEvent(SentryEvent @event)
        {
            // TODO: calculate length ahead of time?
            var headers = new Dictionary<string, object>
            {
                ["type"] = "event"
            };

            return new EnvelopeItem(headers, @event);
        }
    }
}

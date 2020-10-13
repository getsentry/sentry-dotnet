using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Internal;
using Sentry.Internal.Extensions;

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

        /// <summary>
        /// Attempts to extract the value of "length" header if it's present.
        /// </summary>
        public long? TryGetLength()
        {
            if (!Header.TryGetValue(LengthKey, out var value))
            {
                return null;
            }

            if (value is long valueLong)
            {
                return valueLong;
            }

            if (value is int valueInt)
            {
                return valueInt;
            }

            return null;
        }

        private async Task<MemoryStream> BufferPayloadAsync(CancellationToken cancellationToken = default)
        {
            var buffer = new MemoryStream();
            await Payload.SerializeAsync(buffer, cancellationToken).ConfigureAwait(false);
            buffer.Seek(0, SeekOrigin.Begin);

            return buffer;
        }

        /// <inheritdoc />
        public async Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            // Length is known
            if (TryGetLength() != null)
            {
                // Header
                await JsonSerializer.SerializeToStreamAsync(Header, stream, cancellationToken).ConfigureAwait(false);
                stream.WriteByte((byte)'\n');

                // Payload
                await Payload.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
            }
            // Length is NOT known (need to calculate)
            else
            {
                using var payloadBuffer = await BufferPayloadAsync(cancellationToken).ConfigureAwait(false);

                // Header
                var headerWithLength = Header.ToDictionary();
                headerWithLength[LengthKey] = payloadBuffer.Length;
                var headerData = JsonSerializer.SerializeToByteArray(headerWithLength);

                await stream.WriteAsync(headerData, cancellationToken).ConfigureAwait(false);
                stream.WriteByte((byte)'\n');

                // Payload
                await payloadBuffer.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
            }
        }

        private const string TypeKey = "type";
        private const string LengthKey = "length";
        private const string FileNameKey = "file_name";

        public static EnvelopeItem FromFile(string filePath)
        {
            var fileStream = File.OpenRead(filePath);
            var payload = new StreamSerializable(fileStream);

            var header = new Dictionary<string, object>
            {
                [TypeKey] = "attachment",
                [FileNameKey] = Path.GetFileName(filePath),
                [LengthKey] = fileStream.Length
            };

            return new EnvelopeItem(header, payload);
        }

        public static EnvelopeItem FromEvent(SentryEvent @event)
        {
            var header = new Dictionary<string, object>
            {
                [TypeKey] = "event"
            };

            return new EnvelopeItem(header, @event);
        }
    }
}

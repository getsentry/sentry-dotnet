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
        private const string TypeKey = "type";
        private const string LengthKey = "length";
        private const string FileNameKey = "file_name";

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
        /// Attempts to extract the value of "type" header if it's present.
        /// </summary>
        public string? TryGetType() => Header.GetValueOrDefault(TypeKey) as string;

        /// <summary>
        /// Attempts to extract the value of "length" header if it's present.
        /// </summary>
        public long? TryGetLength() =>
            Header.GetValueOrDefault(LengthKey) switch
            {
                long x => x,
                int x => x,
                _ => null
            };

        private async Task<MemoryStream> BufferPayloadAsync(CancellationToken cancellationToken = default)
        {
            var buffer = new MemoryStream();
            await Payload.SerializeAsync(buffer, cancellationToken).ConfigureAwait(false);
            _ = buffer.Seek(0, SeekOrigin.Begin);

            return buffer;
        }

        /// <inheritdoc />
        public async Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            // Length is known
            if (TryGetLength() != null)
            {
                // Header
                await Json.SerializeToStreamAsync(Header, stream, cancellationToken).ConfigureAwait(false);
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
                var headerData = Json.SerializeToByteArray(headerWithLength);

                await stream.WriteAsync(headerData, cancellationToken).ConfigureAwait(false);
                stream.WriteByte((byte)'\n');

                // Payload
                await payloadBuffer.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates an envelope item from file.
        /// </summary>
        public static EnvelopeItem FromFile(string filePath)
        {
            var file = File.OpenRead(filePath);
            var payload = new StreamSerializable(file);

            var header = new Dictionary<string, object>
            {
                [TypeKey] = "attachment",
                [FileNameKey] = Path.GetFileName(filePath),
                [LengthKey] = file.Length
            };

            return new EnvelopeItem(header, payload);
        }

        /// <summary>
        /// Creates an envelope item from text content.
        /// </summary>
        public static EnvelopeItem FromString(string text)
        {
            using var buffer = new MemoryStream(
                Encoding.UTF8.GetBytes(text)
            );

            var payload = new StreamSerializable(buffer);

            var header = new Dictionary<string, object>
            {
                [TypeKey] = "attachment",
                [LengthKey] = buffer.Length
            };

            return new EnvelopeItem(header, payload);
        }

        /// <summary>
        /// Creates an envelope item from an event.
        /// </summary>
        public static EnvelopeItem FromEvent(SentryEvent @event)
        {
            var header = new Dictionary<string, object>
            {
                [TypeKey] = "event"
            };

            return new EnvelopeItem(header, @event);
        }

        /// <summary>
        /// Creates an envelope item from an user feedback.
        /// </summary>
        public static EnvelopeItem FromUserFeedback(UserFeedback sentryUserFeedback)
        {
            var header = new Dictionary<string, object>
            {
                [TypeKey] = "user_report"
            };

            return new EnvelopeItem(header, sentryUserFeedback);
        }
    }
}

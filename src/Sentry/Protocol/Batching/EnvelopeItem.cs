using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol.Batching
{
    internal class EnvelopeItem : ISerializable, IDisposable
    {
        private const string TypeKey = "type";
        private const string TypeValueEvent = "event";
        private const string TypeValueUserReport = "user_report";
        private const string LengthKey = "length";
        private const string FileNameKey = "file_name";

        public IReadOnlyDictionary<string, object> Header { get; }

        public ISerializable Payload { get; }

        public EnvelopeItem(IReadOnlyDictionary<string, object> header, ISerializable payload)
        {
            Header = header;
            Payload = payload;
        }

        public string? TryGetType() => Header.GetValueOrDefault(TypeKey) as string;

        public long? TryGetLength() =>
            Header.GetValueOrDefault(LengthKey) switch
            {
                null => null,
                var value => Convert.ToInt64(value) // can be int, long, or another numeric type
            };

        private async ValueTask<MemoryStream> BufferPayloadAsync(CancellationToken cancellationToken = default)
        {
            var buffer = new MemoryStream();
            await Payload.SerializeAsync(buffer, cancellationToken).ConfigureAwait(false);
            buffer.Seek(0, SeekOrigin.Begin);

            return buffer;
        }

        public async ValueTask SerializeAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            // Length is known
            if (TryGetLength() != null)
            {
                // Header
                await Json.SerializeToStreamAsync(Header, stream, cancellationToken).ConfigureAwait(false);
                await stream.WriteByteAsync((byte)'\n', cancellationToken).ConfigureAwait(false);

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
                await stream.WriteByteAsync((byte)'\n', cancellationToken).ConfigureAwait(false);

                // Payload
                await payloadBuffer.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
            }
        }

        public void Dispose() => (Payload as IDisposable)?.Dispose();

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

        public static EnvelopeItem FromEvent(SentryEvent @event)
        {
            var header = new Dictionary<string, object>
            {
                [TypeKey] = TypeValueEvent
            };

            return new EnvelopeItem(header, new JsonSerializable(@event));
        }

        public static EnvelopeItem FromUserFeedback(UserFeedback sentryUserFeedback)
        {
            var header = new Dictionary<string, object>
            {
                [TypeKey] = TypeValueUserReport
            };

            return new EnvelopeItem(header, new JsonSerializable(sentryUserFeedback));
        }

        private static async ValueTask<IReadOnlyDictionary<string, object>> DeserializeHeaderAsync(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            var buffer = new List<byte>();

            var prevByte = default(int);
            await foreach (var curByte in stream.ReadAllBytesAsync(cancellationToken))
            {
                // Break if found an unescaped newline
                if (curByte == '\n' && prevByte != '\\')
                {
                    break;
                }

                buffer.Add(curByte);
                prevByte = curByte;
            }

            return
                Json.DeserializeFromByteArray<Dictionary<string, object>?>(buffer.ToArray())
                ?? throw new InvalidOperationException("Envelope item header is malformed.");
        }

        private static async ValueTask<ISerializable> DeserializePayloadAsync(
            Stream stream,
            IReadOnlyDictionary<string, object> header,
            CancellationToken cancellationToken = default)
        {
            var payloadLength = header.GetValueOrDefault(LengthKey) switch
            {
                null => (long?)null,
                var value => Convert.ToInt64(value)
            };

            var payloadType = header.GetValueOrDefault(TypeKey) as string;

            // Event
            if (string.Equals(payloadType, TypeValueEvent, StringComparison.OrdinalIgnoreCase))
            {
                var bufferLength = (int)(payloadLength ?? stream.Length);
                var buffer = await stream.ReadByteChunkAsync(bufferLength, cancellationToken).ConfigureAwait(false);

                return new JsonSerializable(Json.DeserializeFromByteArray<SentryEvent>(buffer));
            }

            // User report
            if (string.Equals(payloadType, TypeValueUserReport, StringComparison.OrdinalIgnoreCase))
            {
                var bufferLength = (int)(payloadLength ?? stream.Length);
                var buffer = await stream.ReadByteChunkAsync(bufferLength, cancellationToken).ConfigureAwait(false);

                return new JsonSerializable(Json.DeserializeFromByteArray<UserFeedback>(buffer));
            }

            // Arbitrary payload
            if (payloadLength != null)
            {
                stream.Seek(payloadLength.Value, SeekOrigin.Current);
            }
            else
            {
                stream.Seek(0, SeekOrigin.End);
            }

            var payloadStream = new PartialStream(stream, stream.Position, payloadLength);

            return new StreamSerializable(payloadStream);
        }

        public static async ValueTask<EnvelopeItem> DeserializeAsync(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            var header = await DeserializeHeaderAsync(stream, cancellationToken).ConfigureAwait(false);
            var payload = await DeserializePayloadAsync(stream, header, cancellationToken).ConfigureAwait(false);

            // Swallow trailing newlines (some envelopes may have them after payloads)
            await foreach (var curByte in stream.ReadAllBytesAsync(cancellationToken))
            {
                if (curByte != '\n')
                {
                    stream.Position--;
                    break;
                }
            }

            return new EnvelopeItem(header, payload);
        }
    }
}

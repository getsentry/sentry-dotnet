using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Internal.Extensions;
using Sentry.Protocol.Envelopes;

namespace Sentry
{
    internal class SentryJsonSerializer
    {
        private readonly SentryOptions _options;

        public SentryJsonSerializer(SentryOptions options) => _options = options;

        public async Task SerializeAsync(Envelope envelope, Stream stream, CancellationToken cancellationToken)
        {
            // Header
            await using var writer = new Utf8JsonWriter(stream);
            writer.WriteDictionaryValue(envelope.Header);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            await stream.WriteByteAsync((byte)'\n', cancellationToken).ConfigureAwait(false);

            // Items
            foreach (var item in envelope.Items)
            {
                await SerializeAsync(item, stream, cancellationToken).ConfigureAwait(false);
                await stream.WriteByteAsync((byte)'\n', cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SerializeAsync(EnvelopeItem item, Stream stream, CancellationToken cancellationToken)
        {
            // Length is known
            if (item.TryGetLength() != null)
            {
                // Header
                await using var writer = new Utf8JsonWriter(stream); // TODO: Should this Writer be propagated?
                writer.WriteDictionaryValue(item.Header);
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
                await stream.WriteByteAsync((byte)'\n', cancellationToken).ConfigureAwait(false);

                // Payload
                await item.Payload.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
            }
            // Length is NOT known (need to calculate)
            else
            {
                // using var payloadBuffer = await BufferPayloadAsync(cancellationToken).ConfigureAwait(false);
                var payloadBuffer = new MemoryStream();
                await item.Payload.SerializeAsync(payloadBuffer, cancellationToken).ConfigureAwait(false);
                payloadBuffer.Seek(0, SeekOrigin.Begin);

                // Header
                var headerWithLength = item.Header.ToDictionary();
                // TODO: Can we create a wrapping stream that counts the bytes that go through it,
                // and only wrap it here instead? That will give us the 'length' without the need to buffer
                headerWithLength[EnvelopeItem.LengthKey] = payloadBuffer.Length;

                await using var writer = new Utf8JsonWriter(stream);
                writer.WriteDictionaryValue(headerWithLength);
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false); // TODO: Why flush here?
                await stream.WriteByteAsync((byte)'\n', cancellationToken).ConfigureAwait(false);

                // Payload
                await payloadBuffer.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

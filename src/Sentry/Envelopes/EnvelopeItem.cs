using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol.Envelopes
{
    /// <summary>
    /// Envelope item.
    /// </summary>
    public sealed class EnvelopeItem : ISerializable, IDisposable
    {
        private const string TypeKey = "type";
        private const string TypeValueEvent = "event";
        private const string TypeValueUserReport = "user_report";
        private const string TypeValueTransaction = "transaction";
        private const string TypeValueSession = "session";
        private const string TypeValueAttachment = "attachment";
        private const string LengthKey = "length";
        private const string FileNameKey = "filename";

        /// <summary>
        /// Header associated with this envelope item.
        /// </summary>
        public IReadOnlyDictionary<string, object?> Header { get; }

        /// <summary>
        /// Item payload.
        /// </summary>
        public ISerializable Payload { get; }

        /// <summary>
        /// Initializes an instance of <see cref="EnvelopeItem"/>.
        /// </summary>
        public EnvelopeItem(IReadOnlyDictionary<string, object?> header, ISerializable payload)
        {
            Header = header;
            Payload = payload;
        }

        /// <summary>
        /// Tries to get item type.
        /// </summary>
        public string? TryGetType() => Header.GetValueOrDefault(TypeKey) as string;

        /// <summary>
        /// Tries to get payload length.
        /// </summary>
        public long? TryGetLength() =>
            Header.GetValueOrDefault(LengthKey) switch
            {
                null => null,
                var value => Convert.ToInt64(value) // can be int, long, or another numeric type
            };

        public string? TryGetFileName() => Header.GetValueOrDefault(FileNameKey) as string;

        private async Task<MemoryStream> BufferPayloadAsync(IDiagnosticLogger? logger, CancellationToken cancellationToken = default)
        {
            var buffer = new MemoryStream();
            await Payload.SerializeAsync(buffer, logger, cancellationToken).ConfigureAwait(false);
            buffer.Seek(0, SeekOrigin.Begin);

            return buffer;
        }

        private static async Task SerializeHeaderAsync(
            Stream stream,
            IReadOnlyDictionary<string, object?> header,
            IDiagnosticLogger? logger,
            CancellationToken cancellationToken = default)
        {
            var writer = new Utf8JsonWriter(stream);
#if NET461 || NETSTANDARD2_0
            using (writer)
#else
            await using (writer.ConfigureAwait(false))
#endif
            {
                writer.WriteDictionaryValue(header, logger);
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SerializeHeaderAsync(
            Stream stream,
            IDiagnosticLogger? logger,
            CancellationToken cancellationToken = default) =>
            await SerializeHeaderAsync(stream, Header, logger, cancellationToken).ConfigureAwait(false);

        /// <inheritdoc />
        public async Task SerializeAsync(Stream stream, IDiagnosticLogger? logger, CancellationToken cancellationToken = default)
        {
            // Length is known
            if (TryGetLength() != null)
            {
                // Header
                await SerializeHeaderAsync(stream, logger, cancellationToken).ConfigureAwait(false);
                await stream.WriteByteAsync((byte)'\n', cancellationToken).ConfigureAwait(false);

                // Payload
                await Payload.SerializeAsync(stream, logger, cancellationToken).ConfigureAwait(false);
            }
            // Length is NOT known (need to calculate)
            else
            {
                var payloadBuffer = await BufferPayloadAsync(logger, cancellationToken).ConfigureAwait(false);
#if NET461 || NETSTANDARD2_0
                using (payloadBuffer)
#else
                await using (payloadBuffer.ConfigureAwait(false))
#endif
                {
                    // Header
                    var headerWithLength = Header.ToDictionary();
                    headerWithLength[LengthKey] = payloadBuffer.Length;
                    await SerializeHeaderAsync(stream, headerWithLength, logger, cancellationToken).ConfigureAwait(false);
                    await stream.WriteByteAsync((byte)'\n', cancellationToken).ConfigureAwait(false);

                    // Payload
                    await payloadBuffer.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc />
        public void Dispose() => (Payload as IDisposable)?.Dispose();

        /// <summary>
        /// Creates an envelope item from an event.
        /// </summary>
        public static EnvelopeItem FromEvent(SentryEvent @event)
        {
            var header = new Dictionary<string, object?>(1, StringComparer.Ordinal)
            {
                [TypeKey] = TypeValueEvent
            };

            return new EnvelopeItem(header, new JsonSerializable(@event));
        }

        /// <summary>
        /// Creates an envelope item from user feedback.
        /// </summary>
        public static EnvelopeItem FromUserFeedback(UserFeedback sentryUserFeedback)
        {
            var header = new Dictionary<string, object?>(1, StringComparer.Ordinal)
            {
                [TypeKey] = TypeValueUserReport
            };

            return new EnvelopeItem(header, new JsonSerializable(sentryUserFeedback));
        }

        /// <summary>
        /// Creates an envelope item from transaction.
        /// </summary>
        public static EnvelopeItem FromTransaction(Transaction transaction)
        {
            var header = new Dictionary<string, object?>(1, StringComparer.Ordinal)
            {
                [TypeKey] = TypeValueTransaction
            };

            return new EnvelopeItem(header, new JsonSerializable(transaction));
        }

        /// <summary>
        /// Creates an envelope item from a session update.
        /// </summary>
        public static EnvelopeItem FromSession(SessionUpdate sessionUpdate)
        {
            var header = new Dictionary<string, object?>(1, StringComparer.Ordinal)
            {
                [TypeKey] = TypeValueSession
            };

            return new EnvelopeItem(header, new JsonSerializable(sessionUpdate));
        }

        /// <summary>
        /// Creates an envelope item from attachment.
        /// </summary>
        public static EnvelopeItem FromAttachment(Attachment attachment)
        {
            var stream = attachment.Content.GetStream();

            var attachmentType = attachment.Type switch
            {
                AttachmentType.Minidump => "event.minidump",
                AttachmentType.AppleCrashReport => "event.applecrashreport",
                AttachmentType.UnrealContext => "unreal.context",
                AttachmentType.UnrealLogs => "unreal.logs",
                _ => "event.attachment"
            };

            var header = new Dictionary<string, object?>(5, StringComparer.Ordinal)
            {
                [TypeKey] = TypeValueAttachment,
                [LengthKey] = stream.TryGetLength(),
                [FileNameKey] = attachment.FileName,
                ["attachment_type"] = attachmentType,
                ["content_type"] = attachment.ContentType
            };

            return new EnvelopeItem(header, new StreamSerializable(stream));
        }

        private static async Task<IReadOnlyDictionary<string, object?>> DeserializeHeaderAsync(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            var buffer = new List<byte>();

            var prevByte = default(int);
            await foreach (var curByte in stream.ReadAllBytesAsync(cancellationToken).ConfigureAwait(false))
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
                Json.Parse(buffer.ToArray()).GetDictionaryOrNull()
                ?? throw new InvalidOperationException("Envelope item header is malformed.");
        }

        private static async Task<ISerializable> DeserializePayloadAsync(
            Stream stream,
            IReadOnlyDictionary<string, object?> header,
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
                var json = Json.Parse(buffer);

                return new JsonSerializable(SentryEvent.FromJson(json));
            }

            // User report
            if (string.Equals(payloadType, TypeValueUserReport, StringComparison.OrdinalIgnoreCase))
            {
                var bufferLength = (int)(payloadLength ?? stream.Length);
                var buffer = await stream.ReadByteChunkAsync(bufferLength, cancellationToken).ConfigureAwait(false);
                var json = Json.Parse(buffer);

                return new JsonSerializable(UserFeedback.FromJson(json));
            }

            // Transaction
            if (string.Equals(payloadType, TypeValueTransaction, StringComparison.OrdinalIgnoreCase))
            {
                var bufferLength = (int)(payloadLength ?? stream.Length);
                var buffer = await stream.ReadByteChunkAsync(bufferLength, cancellationToken).ConfigureAwait(false);
                var json = Json.Parse(buffer);

                return new JsonSerializable(Transaction.FromJson(json));
            }

            // Session
            if (string.Equals(payloadType, TypeValueSession, StringComparison.OrdinalIgnoreCase))
            {
                var bufferLength = (int)(payloadLength ?? stream.Length);
                var buffer = await stream.ReadByteChunkAsync(bufferLength, cancellationToken).ConfigureAwait(false);
                var json = Json.Parse(buffer);

                return new JsonSerializable(SessionUpdate.FromJson(json));
            }

            // Arbitrary payload
            var payloadStream = new PartialStream(stream, stream.Position, payloadLength);

            if (payloadLength is not null)
            {
                stream.Seek(payloadLength.Value, SeekOrigin.Current);
            }
            else
            {
                stream.Seek(0, SeekOrigin.End);
            }

            return new StreamSerializable(payloadStream);
        }

        /// <summary>
        /// Deserializes envelope item from stream.
        /// </summary>
        public static async Task<EnvelopeItem> DeserializeAsync(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            var header = await DeserializeHeaderAsync(stream, cancellationToken).ConfigureAwait(false);
            var payload = await DeserializePayloadAsync(stream, header, cancellationToken).ConfigureAwait(false);

            // Swallow trailing newlines (some envelopes may have them after payloads)
            await foreach (var curByte in stream.ReadAllBytesAsync(cancellationToken).ConfigureAwait(false))
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

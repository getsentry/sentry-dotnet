using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol.Envelopes
{
    /// <summary>
    /// Envelope.
    /// </summary>
    internal sealed class Envelope : ISerializable, IDisposable
    {
        private const string EventIdKey = "event_id";

        /// <summary>
        /// Header associated with the envelope.
        /// </summary>
        public IReadOnlyDictionary<string, object?> Header { get; }

        /// <summary>
        /// Envelope items.
        /// </summary>
        public IReadOnlyList<EnvelopeItem> Items { get; }

        /// <summary>
        /// Initializes an instance of <see cref="Envelope"/>.
        /// </summary>
        public Envelope(IReadOnlyDictionary<string, object?> header, IReadOnlyList<EnvelopeItem> items)
        {
            Header = header;
            Items = items;
        }

        /// <summary>
        /// Attempts to extract the value of "sentry_id" header if it's present.
        /// </summary>
        public SentryId? TryGetEventId() =>
            Header.TryGetValue(EventIdKey, out var value) &&
            value is string valueString &&
            Guid.TryParse(valueString, out var guid)
                ? new SentryId(guid)
                : null;

        private async Task SerializeHeaderAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            await using var writer = new Utf8JsonWriter(stream);
            writer.WriteDictionaryValue(Header);
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            // Header
            await SerializeHeaderAsync(stream, cancellationToken).ConfigureAwait(false);
            await stream.WriteByteAsync((byte)'\n', cancellationToken).ConfigureAwait(false);

            // Items
            foreach (var item in Items)
            {
                await item.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
                await stream.WriteByteAsync((byte)'\n', cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public void Dispose() => Items.DisposeAll();

        private static Dictionary<string, object?> CreateHeader(SentryId? eventId = null)
        {
            var header = new Dictionary<string, object?>(2, StringComparer.Ordinal)
            {
                // Include limited SDK information (no packages)
                ["sdk"] = new Dictionary<string, string?>(2, StringComparer.Ordinal)
                {
                    ["name"] = SdkVersion.Instance.Name,
                    ["version"] = SdkVersion.Instance.Version
                }
            };

            if (eventId is not null)
            {
                header[EventIdKey] = eventId.Value.ToString();
            }

            return header;
        }

        /// <summary>
        /// Creates an envelope that contains a single event.
        /// </summary>
        public static Envelope FromEvent(
            SentryEvent @event,
            IReadOnlyCollection<Attachment>? attachments = null,
            SessionUpdate? sessionUpdate = null)
        {
            var header = CreateHeader(@event.EventId);

            var items = new List<EnvelopeItem>
            {
                EnvelopeItem.FromEvent(@event)
            };

            if (attachments is not null)
            {
                items.AddRange(attachments.Select(EnvelopeItem.FromAttachment));
            }

            if (sessionUpdate is not null)
            {
                items.Add(EnvelopeItem.FromSession(sessionUpdate));
            }

            return new Envelope(header, items);
        }

        /// <summary>
        /// Creates an envelope that contains a single user feedback.
        /// </summary>
        public static Envelope FromUserFeedback(UserFeedback sentryUserFeedback)
        {
            var header = CreateHeader(sentryUserFeedback.EventId);

            var items = new[]
            {
                EnvelopeItem.FromUserFeedback(sentryUserFeedback)
            };

            return new Envelope(header, items);
        }

        /// <summary>
        /// Creates an envelope that contains a single transaction.
        /// </summary>
        public static Envelope FromTransaction(Transaction transaction)
        {
            var header = CreateHeader(transaction.EventId);

            var items = new[]
            {
                EnvelopeItem.FromTransaction(transaction)
            };

            return new Envelope(header, items);
        }

        /// <summary>
        /// Creates an envelope that contains a session update.
        /// </summary>
        public static Envelope FromSession(SessionUpdate sessionUpdate)
        {
            var header = CreateHeader();

            var items = new[]
            {
                EnvelopeItem.FromSession(sessionUpdate)
            };

            return new Envelope(header, items);
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
                ?? throw new InvalidOperationException("Envelope header is malformed.");
        }

        /// <summary>
        /// Deserializes envelope from stream.
        /// </summary>
        public static async Task<Envelope> DeserializeAsync(
            Stream stream,
            CancellationToken cancellationToken = default)
        {
            var header = await DeserializeHeaderAsync(stream, cancellationToken).ConfigureAwait(false);

            var items = new List<EnvelopeItem>();
            while (stream.Position < stream.Length)
            {
                var item = await EnvelopeItem.DeserializeAsync(stream, cancellationToken).ConfigureAwait(false);
                items.Add(item);
            }

            return new Envelope(header, items);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Protocol
{
    /// <summary>
    /// Envelope.
    /// </summary>
    public class Envelope : ISerializable
    {
        /// <summary>
        /// Headers associated with this envelope.
        /// </summary>
        public EnvelopeHeaderCollection Headers { get; }

        /// <summary>
        /// Items inside this envelope.
        /// </summary>
        public EnvelopeItemCollection Items { get; }

        /// <summary>
        /// Initializes an instance of <see cref="Envelope"/>.
        /// </summary>
        public Envelope(EnvelopeHeaderCollection headers, EnvelopeItemCollection items)
        {
            Headers = headers;
            Items = items;
        }

        /// <summary>
        /// Attempts to extract the value of "sentry_id" header if it's present.
        /// </summary>
        public SentryId? TryGetEventId() =>
            Headers.KeyValues.TryGetValue("event_id", out var value) &&
            value is string valueString &&
            Guid.TryParse(valueString, out var guid)
                ? new SentryId(guid)
                : (SentryId?)null;

        /// <inheritdoc />
        public async Task SerializeAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            await Headers.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
            stream.WriteByte((byte)'\n');

            if (Items.Count > 0)
            {
                await Items.SerializeAsync(stream, cancellationToken).ConfigureAwait(false);
                stream.WriteByte((byte)'\n');
            }
        }

        public static Envelope FromEvent(SentryEvent @event)
        {
            var headers = new EnvelopeHeaderCollection(new Dictionary<string, object>
            {
                ["event_id"] = @event.EventId.ToString()
            });

            var items = new EnvelopeItemCollection(new[]
            {
                EnvelopeItem.FromEvent(@event)
            });

            return new Envelope(headers, items);
        }
    }
}

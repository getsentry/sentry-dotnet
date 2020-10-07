using System;
using System.Text;

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
            value is string valueString
                ? new SentryId(Guid.Parse(valueString))
                : (SentryId?)null;

        /// <inheritdoc />
        public string Serialize()
        {
            var buffer = new StringBuilder();

            buffer.Append(Headers.Serialize());
            buffer.Append('\n');

            if (Items.Count > 0)
            {
                buffer.Append(Items.Serialize());
                buffer.Append('\n');
            }

            return buffer.ToString();
        }

        /// <inheritdoc />
        public override string ToString() => Serialize();
    }
}

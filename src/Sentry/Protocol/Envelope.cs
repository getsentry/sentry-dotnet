using System;
using System.Text;

namespace Sentry.Protocol
{
    public class Envelope : ISerializable
    {
        public EnvelopeHeaderCollection Headers { get; }

        public EnvelopeItemCollection Items { get; }

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

        public override string ToString() => Serialize();
    }
}

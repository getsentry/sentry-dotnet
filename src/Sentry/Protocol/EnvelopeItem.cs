using System.Text;

namespace Sentry.Protocol
{
    /// <summary>
    /// Envelope item.
    /// </summary>
    public class EnvelopeItem : ISerializable
    {
        /// <summary>
        /// Headers associated with this item.
        /// </summary>
        public EnvelopeHeaderCollection Headers { get; }

        /// <summary>
        /// Payload associated with this item.
        /// </summary>
        public EnvelopePayload Payload { get; }

        /// <summary>
        /// Initializes an instance of <see cref="EnvelopeItem"/>.
        /// </summary>
        public EnvelopeItem(EnvelopeHeaderCollection headers, EnvelopePayload payload)
        {
            Headers = headers;
            Payload = payload;
        }

        /// <inheritdoc />
        public string Serialize()
        {
            var buffer = new StringBuilder();

            buffer.Append(Headers.Serialize());
            buffer.Append('\n');
            buffer.Append(Payload.Serialize());

            return buffer.ToString();
        }

        /// <inheritdoc />
        public override string ToString() => Serialize();
    }
}

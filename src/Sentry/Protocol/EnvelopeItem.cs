using System.Text;

namespace Sentry.Protocol
{
    public class EnvelopeItem : ISerializable
    {
        public EnvelopeHeaderCollection Headers { get; }

        public EnvelopePayload Payload { get; }

        public EnvelopeItem(EnvelopeHeaderCollection headers, EnvelopePayload payload)
        {
            Headers = headers;
            Payload = payload;
        }

        public string Serialize()
        {
            var buffer = new StringBuilder();

            buffer.Append(Headers.Serialize());
            buffer.Append('\n');
            buffer.Append(Payload.Serialize());

            return buffer.ToString();
        }

        public override string ToString() => Serialize();
    }
}

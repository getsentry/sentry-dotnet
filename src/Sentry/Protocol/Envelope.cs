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

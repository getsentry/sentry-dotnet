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

        public string Serialize() => string.Concat(
            Headers.Serialize(),
            "\n",
            Items.Serialize(),
            "\n"
        );

        public override string ToString() => Serialize();
    }
}

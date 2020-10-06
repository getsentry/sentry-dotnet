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

        public string Serialize() => string.Concat(
            Headers.Serialize(),
            "\n",
            Payload.Serialize(),
            "\n"
        );

        public override string ToString() => Serialize();
    }
}

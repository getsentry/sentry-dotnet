using System.Text;

namespace Sentry.Protocol
{
    public class EnvelopePayload : ISerializable
    {
        public byte[] Data { get; }

        public EnvelopePayload(byte[] data)
        {
            Data = data;
        }

        public string Serialize() => Encoding.ASCII.GetString(Data);

        public override string ToString() => Serialize();
    }
}

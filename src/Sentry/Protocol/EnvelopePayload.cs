using System.Text;

namespace Sentry.Protocol
{
    /// <summary>
    /// Envelope payload.
    /// </summary>
    public class EnvelopePayload : ISerializable
    {
        /// <summary>
        /// Payload data.
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// Initializes an instance of <see cref="EnvelopePayload"/>.
        /// </summary>
        public EnvelopePayload(byte[] data)
        {
            Data = data;
        }

        /// <inheritdoc />
        public string Serialize() => Encoding.UTF8.GetString(Data);

        /// <inheritdoc />
        public override string ToString() => Serialize();
    }
}

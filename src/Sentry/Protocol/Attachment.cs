using Sentry.Protocol.Envelopes;

namespace Sentry.Protocol
{
    /// <summary>
    /// An attachment to send to Sentry.
    /// </summary>
    public class Attachment
    {
        // Either these two are not null
        private readonly byte[]? _bytes;
        private readonly string? _name;

        // Or this is not null
        private readonly string? _filePath;

        /// <summary>
        /// Creates an attachment from a file path.
        /// </summary>
        /// <param name="filePath">The path to the file to attach.</param>
        public Attachment(string filePath) => _filePath = filePath;

        /// <summary>
        /// Creates an attachment from a byte array and a name.
        /// </summary>
        /// <param name="bytes">The bytes to send as attachment.</param>
        /// <param name="name">The name of the attachment.</param>
        public Attachment(byte[] bytes, string name)
        {
            _bytes = bytes;
            _name = name;
        }

        internal EnvelopeItem ToEnvelopeItem() =>
            _filePath is not null
                ? EnvelopeItem.FromFile(_filePath)
                : EnvelopeItem.FromBytes(_bytes!, _name!);
    }
}

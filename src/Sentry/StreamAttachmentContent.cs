using System.IO;

namespace Sentry
{
    /// <summary>
    /// Attachment sourced from stream.
    /// </summary>
    public class StreamAttachmentContent : IAttachmentContent
    {
        private readonly Stream _stream;

        /// <summary>
        /// Creates a new instance of <see cref="StreamAttachmentContent"/>.
        /// </summary>
        public StreamAttachmentContent(Stream stream) => _stream = stream;

        /// <inheritdoc />
        public Stream GetStream() => _stream;
    }
}

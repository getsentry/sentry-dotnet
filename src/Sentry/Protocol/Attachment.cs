using System;
using System.IO;

namespace Sentry.Protocol
{
    /// <summary>
    /// Sentry attachment.
    /// </summary>
    public class Attachment : IDisposable
    {
        /// <summary>
        /// Attachment stream.
        /// </summary>
        public Stream Stream { get; }

        /// <summary>
        /// Attachment file name.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Attachment size.
        /// </summary>
        public long? Length { get; }

        /// <summary>
        /// Initializes an instance of <see cref="Attachment"/>.
        /// </summary>
        public Attachment(Stream stream, string fileName, long? length)
        {
            Stream = stream;
            FileName = fileName;
            Length = length;
        }

        /// <inheritdoc />
        public void Dispose() => Stream.Dispose();
    }
}

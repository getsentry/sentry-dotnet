using System;
using System.IO;

namespace Sentry.Protocol
{
    /// <summary>
    /// Sentry attachment.
    /// </summary>
    public class Attachment : IDisposable
    {
        private readonly Stream _stream;
        private readonly string _fileName;

        /// <summary>
        /// Initializes an instance of <see cref="Attachment"/>.
        /// </summary>
        public Attachment(Stream stream, string fileName)
        {
            _stream = stream;
            _fileName = fileName;
        }

        /// <inheritdoc />
        public void Dispose() => _stream.Dispose();
    }
}

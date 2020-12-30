using System;
using System.Diagnostics;
using System.IO;

namespace Sentry.Protocol
{
    /// <summary>
    /// Attachment type.
    /// </summary>
    public enum AttachmentType
    {
        /// <summary>
        /// Standard attachment without special meaning.
        /// </summary>
        Default,

        /// <summary>
        /// Minidump file that creates an error event and is symbolicated.
        /// The file should start with the <code>MDMP</code> magic bytes.
        /// </summary>
        Minidump,

        /// <summary>
        /// Apple crash report file that creates an error event and is symbolicated.
        /// </summary>
        AppleCrashReport,

        /// <summary>
        /// XML file containing UE4 crash meta data.
        /// During event ingestion, event contexts and extra fields are extracted from this file.
        /// </summary>
        UnrealContext,

        /// <summary>
        /// Plain-text log file obtained from UE4 crashes.
        /// During event ingestion, the last logs are extracted into event breadcrumbs.
        /// </summary>
        UnrealLogs
    }

    /// <summary>
    /// Sentry attachment.
    /// </summary>
    [DebuggerDisplay("{" + nameof(FileName) + "}")]
    public class Attachment : IDisposable
    {
        /// <summary>
        /// Attachment type.
        /// </summary>
        public AttachmentType Type { get; }

        /// <summary>
        /// Attachment stream.
        /// </summary>
        public Stream Stream { get; }

        /// <summary>
        /// Attachment size.
        /// </summary>
        public long Length { get; }

        /// <summary>
        /// Attachment file name.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Attachment content type.
        /// </summary>
        public string? ContentType { get; }

        /// <summary>
        /// Initializes an instance of <see cref="Attachment"/>.
        /// </summary>
        public Attachment(AttachmentType type,
            Stream stream,
            long length,
            string fileName,
            string? contentType)
        {
            Type = type;
            Stream = stream;
            Length = length;
            FileName = fileName;
            ContentType = contentType;
        }

        /// <inheritdoc />
        public void Dispose() => Stream.Dispose();
    }
}

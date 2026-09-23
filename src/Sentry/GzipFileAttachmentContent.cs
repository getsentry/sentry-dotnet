using System.IO.Compression;
using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Attachment sourced from the file system and gzip-compressed while the envelope is written.
/// </summary>
/// <remarks>
/// The file is opened when the attachment is captured but only read, and compressed, when the
/// envelope is serialized, which the SDK does on its background worker. Because this is a
/// <see cref="FileAttachmentContent"/>, scope observers that sync attachments to a native SDK
/// still receive the path of the uncompressed file.
/// </remarks>
public class GzipFileAttachmentContent : FileAttachmentContent
{
    /// <summary>
    /// The compression level used when the file is read.
    /// </summary>
    public CompressionLevel CompressionLevel { get; }

    /// <summary>
    /// Creates a new instance of <see cref="GzipFileAttachmentContent"/>.
    /// </summary>
    /// <param name="filePath">The path to the file to attach.</param>
    public GzipFileAttachmentContent(string filePath) : this(filePath, CompressionLevel.Optimal)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="GzipFileAttachmentContent"/>.
    /// </summary>
    /// <param name="filePath">The path to the file to attach.</param>
    /// <param name="compressionLevel">The compression level used when the file is read.</param>
    public GzipFileAttachmentContent(string filePath, CompressionLevel compressionLevel)
        : base(filePath, readFileAsynchronously: false, deleteOnClose: false)
    {
        CompressionLevel = compressionLevel;
    }

    /// <inheritdoc />
    public override Stream GetStream() => new GzipReadStream(base.GetStream(), CompressionLevel);
}

namespace Sentry;

/// <summary>
/// Attachment sourced from a provided byte array.
/// </summary>
public class ByteAttachmentContent : IAttachmentContent
{
    /// <summary>
    /// The raw bytes of the attachment.
    /// </summary>
    internal byte[] Bytes { get; }

    /// <summary>
    /// Creates a new instance of <see cref="ByteAttachmentContent"/>.
    /// </summary>
    public ByteAttachmentContent(byte[] bytes) => Bytes = bytes;

    /// <inheritdoc />
    public Stream GetStream() => new MemoryStream(Bytes);
}

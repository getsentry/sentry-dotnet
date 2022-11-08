namespace Sentry;

/// <summary>
/// Attachment sourced from a provided byte array.
/// </summary>
public class ByteAttachmentContent : IAttachmentContent
{
    private readonly byte[] _bytes;

    /// <summary>
    /// Creates a new instance of <see cref="ByteAttachmentContent"/>.
    /// </summary>
    public ByteAttachmentContent(byte[] bytes) => _bytes = bytes;

    /// <inheritdoc />
    public Stream GetStream() => new MemoryStream(_bytes);
}

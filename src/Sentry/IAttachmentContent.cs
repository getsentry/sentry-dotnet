namespace Sentry;

/// <summary>
/// Attachment content.
/// </summary>
public interface IAttachmentContent
{
    /// <summary>
    /// Gets the stream that represents attachment content.
    /// </summary>
    public Stream GetStream();
}

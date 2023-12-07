namespace Sentry.Maui.Internal;

internal class ScreenshotAttachment : Attachment
{
    public ScreenshotAttachment()
        : this(
            AttachmentType.Default,
            new ScreenshotAttachmentContent(),
            "screenshot.jpg",
            "image/jpeg")
    {
    }

    private ScreenshotAttachment(
        AttachmentType type,
        IAttachmentContent content,
        string fileName,
        string? contentType)
        : base(type, content, fileName, contentType)
    {
    }
}

internal class ScreenshotAttachmentContent : IAttachmentContent
{
    public Stream GetStream()
    {
        if (Screenshot.Default.IsCaptureSupported)
        {
            var screen = Screenshot.Default.CaptureAsync().ConfigureAwait(false).GetAwaiter().GetResult();

            var stream = screen.OpenReadAsync(ScreenshotFormat.Jpeg).ConfigureAwait(false).GetAwaiter().GetResult();

            return stream;
        }

        return MemoryStream.Null;
    }
}

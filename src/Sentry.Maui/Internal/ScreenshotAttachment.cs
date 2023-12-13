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
        try
        {
#if !WINDOWS
            if (Screenshot.Default.IsCaptureSupported)
            {
                // This actually runs synchronously (returning Task.FromResult) on the following platforms:
                // Android: https://github.com/dotnet/maui/blob/3c7b65264d2f341a48db32263a271fd8718cfd23/src/Essentials/src/Screenshot/Screenshot.android.cs#L49
                // iOS: https://github.com/dotnet/maui/blob/3c7b65264d2f341a48db32263a271fd8718cfd23/src/Essentials/src/Screenshot/Screenshot.ios.cs#L49
                var screen = Screenshot.Default.CaptureAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                var stream = screen.OpenReadAsync(ScreenshotFormat.Jpeg).ConfigureAwait(false).GetAwaiter().GetResult();

                return stream;
            }
#endif
        }
        //In some cases screen capture can throw, for example on Android if the activity is marked as secure.
        catch (Exception)
        {
            return MemoryStream.Null;
        }

        return MemoryStream.Null;
    }
}

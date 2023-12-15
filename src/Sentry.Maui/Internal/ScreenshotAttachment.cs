namespace Sentry.Maui.Internal;

internal class ScreenshotAttachment : Attachment
{
    public ScreenshotAttachment(SentryMauiOptions options)
        : this(
            AttachmentType.Default,
            new ScreenshotAttachmentContent(options),
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
    private readonly SentryMauiOptions _options;

    public ScreenshotAttachmentContent(SentryMauiOptions options)
    {
        _options = options;
    }

    public Stream GetStream()
    {
        // Not including this on Windows specific build because on WinUI this can deadlock.
#if !WINDOWS
        var stream = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                if (Screenshot.Default.IsCaptureSupported)
                {
                    var screen = await Screenshot.Default.CaptureAsync().ConfigureAwait(true);

                    var stream = await screen.OpenReadAsync(ScreenshotFormat.Jpeg).ConfigureAwait(true);

                    return stream;
                }
                else
                {
                    _options.DiagnosticLogger?.Log(SentryLevel.Warning, "Capturing screenshot not supported");
                    return Stream.Null;
                }
            }
            //In some cases screen capture can throw, for example on Android if the activity is marked as secure.
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.Log(SentryLevel.Error, "Error capturing screenshot", ex);
                return Stream.Null;
            }
        }).ConfigureAwait(false).GetAwaiter().GetResult();
#endif

        return Stream.Null;
    }
}

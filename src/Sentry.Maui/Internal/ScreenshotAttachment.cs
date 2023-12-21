using Sentry.Extensibility;

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
        var stream = Stream.Null;
        // Not including this on Windows specific build because on WinUI this can deadlock.
#if __ANDROID__ || __IOS__
        Stream CaptureScreenBlocking()
        {
            // This actually runs synchronously (returning Task.FromResult) on the following platforms:
            // Android: https://github.com/dotnet/maui/blob/3c7b65264d2f341a48db32263a271fd8718cfd23/src/Essentials/src/Screenshot/Screenshot.android.cs#L49
            // iOS: https://github.com/dotnet/maui/blob/3c7b65264d2f341a48db32263a271fd8718cfd23/src/Essentials/src/Screenshot/Screenshot.ios.cs#L49
            var screen = Screenshot.Default.CaptureAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            return screen.OpenReadAsync(ScreenshotFormat.Jpeg).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        if (!Screenshot.Default.IsCaptureSupported)
        {
            _options.LogDebug("Capturing screenshot not supported");
            return stream;
        }

        if (MainThread.IsMainThread)
        {
            stream = CaptureScreenBlocking();
        }
        else
        {
#if __ANDROID__ //Android does not require UI thread to capture screen but iOS does.
            stream = CaptureScreenBlocking();
#else
            stream = MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    var screen = await Screenshot.Default.CaptureAsync().ConfigureAwait(true);
    
                    return await screen.OpenReadAsync(ScreenshotFormat.Jpeg).ConfigureAwait(true);
                }
                //In some cases screen capture can throw, for example on Android if the activity is marked as secure.
                catch (Exception ex)
                {
                    _options.LogError(ex, "Error capturing screenshot");
                    return Stream.Null;
                }
            }).ConfigureAwait(false).GetAwaiter().GetResult();
#endif
        }
#endif
        return stream;
    }
}

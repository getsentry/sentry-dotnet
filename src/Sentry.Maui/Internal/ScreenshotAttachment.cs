#if  __ANDROID__
using Android.Util;
#endif
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

#if __ANDROID__
                    Log.Warn("Sentry", "Got screenshot stream with length: " + stream.Length);
#endif

                    return stream;
                }
                else
                {
#if __ANDROID__
                    Log.Warn("Sentry", "Capturing screenshot not supported");
#endif
                    _options.LogWarning("Capturing screenshot not supported");
                    return Stream.Null;
                }
            }
            //In some cases screen capture can throw, for example on Android if the activity is marked as secure.
            catch (Exception ex)
            {
#if __ANDROID__
                Log.Warn("Sentry", ex.ToString());
#endif
                _options.LogError(ex, "Error capturing screenshot");
                return Stream.Null;
            }
        }).ConfigureAwait(false).GetAwaiter().GetResult();

        return stream;
#else
        return Stream.Null;
#endif
    }
}

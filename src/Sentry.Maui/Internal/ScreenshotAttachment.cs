using Sentry.Extensibility;

namespace Sentry.Maui.Internal;

internal class ScreenshotAttachment : SentryAttachment
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

#if NET9_0_OR_GREATER && ANDROID
    private static readonly Lock JniLock = new();
#elif ANDROID
    private static readonly object JniLock = new();
#endif

    public ScreenshotAttachmentContent(SentryMauiOptions options)
    {
        _options = options;
    }

    public Stream GetStream()
    {
        var stream = Stream.Null;
        // Not including this on Windows specific build because on WinUI this can deadlock.
#if !(__ANDROID__ || __IOS__)
        return stream;
#endif
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

#if __IOS__
        if (MainThread.IsMainThread)
        {
            stream = CaptureScreenBlocking();
        }
        else
        {
            // Screenshots have to be captured from the UI thread on iOS
            stream = MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var screen = await Screenshot.Default.CaptureAsync().ConfigureAwait(true);

                return await screen.OpenReadAsync(ScreenshotFormat.Jpeg).ConfigureAwait(true);
            }).ConfigureAwait(false).GetAwaiter().GetResult();
        }
#else
        // Capturing screenshots is not threadsafe on Android
        lock (JniLock)
        {
            stream = CaptureScreenBlocking();
        }
#endif
        return stream;
    }
}

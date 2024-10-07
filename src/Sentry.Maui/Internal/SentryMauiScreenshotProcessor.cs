using Sentry.Extensibility;

namespace Sentry.Maui.Internal;

internal class SentryMauiScreenshotProcessor : ISentryEventProcessorWithHint
{
    private readonly SentryMauiOptions _options;

    public SentryMauiScreenshotProcessor(SentryMauiOptions options)
    {
        _options = options;
    }

    public SentryEvent? Process(SentryEvent @event)
    {
        return @event;
    }

    public SentryEvent? Process(SentryEvent @event, SentryHint hint)
    {
        // Call back before taking the screenshot if the callback is mentioned and if a Screenshot is attached
        _options.BeforeCaptureScreenshot();

        hint.Attachments.Add(new ScreenshotAttachment(_options));
        return @event;
    }
}

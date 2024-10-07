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
        // Call back before taking the screenshot if the callback is not null
        if (_options.BeforeCaptureScreenshotInternal != null)
            _options.BeforeCaptureScreenshotInternal.Invoke(@event, hint);

        hint.Attachments.Add(new ScreenshotAttachment(_options));
        return @event;
    }
}

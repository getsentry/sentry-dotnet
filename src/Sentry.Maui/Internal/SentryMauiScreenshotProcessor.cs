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
        if (!_options.BeforeCaptureScreenshotInternal?.Invoke(@event, hint) ?? false)
        {
            // We basically bypass the regular process in favour of the one present in the callback
            return @event;
        }

        hint.Attachments.Add(new ScreenshotAttachment(_options));
        return @event;
    }
}

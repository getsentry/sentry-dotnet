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
        if (!_options.BeforeCaptureInternal?.Invoke(@event, hint) ?? false)
        {
            return @event;
        }

        hint.Attachments.Add(new ScreenshotAttachment(_options));
        return @event;
    }
}

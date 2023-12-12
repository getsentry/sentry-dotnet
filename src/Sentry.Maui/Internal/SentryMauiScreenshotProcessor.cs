using Sentry.Extensibility;

namespace Sentry.Maui.Internal;

internal class SentryMauiScreenshotProcessor : ISentryEventProcessorWithHint
{
    public SentryEvent? Process(SentryEvent @event)
    {
        return @event;
    }

    public SentryEvent? Process(SentryEvent @event, Hint hint)
    {
        hint.Attachments.Add(new ScreenshotAttachment());
        return @event;
    }
}

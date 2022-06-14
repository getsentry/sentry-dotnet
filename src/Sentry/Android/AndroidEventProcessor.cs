using Sentry.Android.Extensions;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Android;

internal class AndroidEventProcessor : ISentryEventProcessor, IDisposable
{
    private readonly Java.IEventProcessor _androidProcessor;
    private readonly Java.Hint _hint = new();

    public AndroidEventProcessor(SentryAndroidOptions androidOptions)
    {
        _androidProcessor = androidOptions.EventProcessors.OfType<JavaObject>()
            .Where(x => x.Class.Name == "io.sentry.android.core.DefaultAndroidEventProcessor")
            .Cast<Java.IEventProcessor>()
            .First();
    }

    public SentryEvent Process(SentryEvent @event)
    {
        // Run a fake event through the Android processor, so we can get context info from the Android SDK.
        // We'll want to do this every time, so that all information is current. (ex: device orientation)
        using var e = new Java.SentryEvent();
        _androidProcessor.Process(e, _hint);

        // Copy what we need to the managed event
        e.Contexts.Device?.ApplyTo(@event.Contexts.Device);

        return @event;
    }

    public void Dispose()
    {
        _androidProcessor.Dispose();
        _hint.Dispose();
    }
}

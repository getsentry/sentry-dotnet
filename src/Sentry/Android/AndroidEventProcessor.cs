using Sentry.Android.Extensions;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Android;

internal class AndroidEventProcessor : ISentryEventProcessor, IDisposable
{
    private readonly Java.IEventProcessor? _androidProcessor;
    private readonly Java.Hint _hint = new();

    public AndroidEventProcessor(SentryAndroidOptions androidOptions)
    {
        _androidProcessor = androidOptions.EventProcessors.OfType<JavaObject>()
            .Where(x => x.Class.Name == "io.sentry.android.core.DefaultAndroidEventProcessor")
            .Cast<Java.IEventProcessor>()
            .FirstOrDefault();
    }

    public SentryEvent Process(SentryEvent @event)
    {
        // Get what information we can ourselves first
        @event.Contexts.Device.ApplyFromAndroidRuntime();

        // Copy more information from the Android SDK
        if (_androidProcessor is { } androidProcessor)
        {
            // TODO: Can we gather more device data directly and remove this?

            // Run a fake event through the Android processor, so we can get context info from the Android SDK.
            // We'll want to do this every time, so that all information is current. (ex: device orientation)
            using var e = new Java.SentryEvent();
            androidProcessor.Process(e, _hint);

            // Copy what we need to the managed event
            if (e.Contexts.Device is { } device)
            {
                @event.Contexts.Device.ApplyFromSentryAndroidSdk(device);
            }
        }

        return @event;
    }

    public void Dispose()
    {
        _androidProcessor?.Dispose();
        _hint.Dispose();
    }
}

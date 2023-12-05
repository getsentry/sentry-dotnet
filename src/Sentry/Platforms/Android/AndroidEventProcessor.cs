using Sentry.Android.Extensions;
using Sentry.Extensibility;
using Sentry.JavaSdk.Android.Core;

namespace Sentry.Android;

internal class AndroidEventProcessor : ISentryEventProcessor, IDisposable
{
    private readonly JavaSdk.IEventProcessor? _androidProcessor;
    private readonly JavaSdk.Hint _hint = new();

    public AndroidEventProcessor(SentryAndroidOptions nativeOptions)
    {
        // Locate the Android SDK's default event processor by its class
        // NOTE: This approach avoids hardcoding the class name (which could be obfuscated by proguard)
        _androidProcessor = nativeOptions.EventProcessors.OfType<JavaObject>()
            .Where(o => o.Class == JavaClass.FromType(typeof(DefaultAndroidEventProcessor)))
            .Cast<JavaSdk.IEventProcessor>()
            .FirstOrDefault();

        // TODO: This would be cleaner, but doesn't compile. Figure out why.
        // _androidProcessor = nativeOptions.EventProcessors
        //     .OfType<DefaultAndroidEventProcessor>()
        //     .FirstOrDefault();
    }

    public SentryEvent Process(SentryEvent @event)
    {
        // Get what information we can ourselves first
        @event.Contexts.Device.ApplyFromAndroidRuntime();
        @event.Contexts.OperatingSystem.ApplyFromAndroidRuntime();

        // Copy more information from the Android SDK
        if (_androidProcessor is { } androidProcessor)
        {
            // TODO: Can we gather more data directly and remove this?

            // Run a fake event through the Android processor, so we can get context info from the Android SDK.
            // We'll want to do this every time, so that all information is current. (ex: device orientation)
            using var e = new JavaSdk.SentryEvent();
            androidProcessor.Process(e, _hint);

            // Copy what we need to the managed event
            if (e.Contexts.Device is { } device)
            {
                @event.Contexts.Device.ApplyFromSentryAndroidSdk(device);
            }
            if (e.Contexts.OperatingSystem is { } os)
            {
                @event.Contexts.OperatingSystem.ApplyFromSentryAndroidSdk(os);
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

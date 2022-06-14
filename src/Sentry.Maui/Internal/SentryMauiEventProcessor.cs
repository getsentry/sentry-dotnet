using Sentry.Extensibility;

namespace Sentry.Maui.Internal;

internal class SentryMauiEventProcessor : ISentryEventProcessor
{
    public SentryEvent Process(SentryEvent @event)
    {
        @event.Sdk.Name = Constants.SdkName;
        @event.Sdk.Version = Constants.SdkVersion;
        @event.Contexts.Device.ApplyMauiDeviceData();

        return @event;
    }
}

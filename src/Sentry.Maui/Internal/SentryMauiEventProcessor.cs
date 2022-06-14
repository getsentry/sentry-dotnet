using Sentry.Extensibility;

namespace Sentry.Maui.Internal;

internal class SentryMauiEventProcessor : ISentryEventProcessor
{
    public SentryEvent Process(SentryEvent @event)
    {
        // Set SDK name and version for MAUI
        @event.Sdk.Name = Constants.SdkName;
        @event.Sdk.Version = Constants.SdkVersion;

        return @event;
    }
}

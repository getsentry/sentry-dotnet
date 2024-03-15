using Sentry.Extensibility;

namespace Sentry.Maui.Internal;

internal class SentryMauiEventProcessor : ISentryEventProcessor
{
    public static bool? InForeground { get; set; }

    private readonly SentryMauiOptions _options;

    public SentryMauiEventProcessor(SentryMauiOptions options)
    {
        _options = options;
    }

    public SentryEvent Process(SentryEvent @event)
    {
        @event.Sdk.Name = Constants.SdkName;
        @event.Sdk.Version = Constants.SdkVersion;
        @event.Contexts.Device.ApplyMauiDeviceData(_options.DiagnosticLogger);
        @event.Contexts.OperatingSystem.ApplyMauiOsData(_options.DiagnosticLogger);
        @event.Contexts.App.InForeground = InForeground;

        return @event;
    }
}

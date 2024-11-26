using Sentry.Extensibility;

namespace Sentry.Maui.Internal;

internal class SentryMauiEventProcessor : ISentryEventProcessor
{
    public static bool? InForeground { get; set; }
    private readonly DisplayInfo _displayInfo;

    private readonly SentryMauiOptions _options;
    private readonly bool _deviceSupportsVibration;
    private readonly bool _deviceSupportsAccelerometer;
    private readonly bool _deviceSupportsGyroscope;
    private readonly IDeviceInfo _deviceInfo;
    private readonly string _deviceIdiom;

    public SentryMauiEventProcessor(SentryMauiOptions options)
    {
        _options = options;

        // https://docs.microsoft.com/dotnet/maui/platform-integration/device/display
        _displayInfo = DeviceDisplay.MainDisplayInfo;

#if !PLATFORM_NEUTRAL

        // https://docs.microsoft.com/dotnet/maui/platform-integration/device/vibrate
        _deviceSupportsVibration = Vibration.Default.IsSupported;

        // https://docs.microsoft.com/dotnet/maui/platform-integration/device/sensors
        _deviceSupportsAccelerometer = Accelerometer.IsSupported;
        _deviceSupportsGyroscope = Gyroscope.IsSupported;
#endif

        // https://docs.microsoft.com/dotnet/maui/platform-integration/device/information
        _deviceInfo = DeviceInfo.Current;
        _deviceIdiom = _deviceInfo.Idiom.ToString();

    }

    public SentryEvent Process(SentryEvent @event)
    {
        @event.Sdk.Name = Constants.SdkName;
        @event.Sdk.Version = Constants.SdkVersion;

        // Apply Device Data
        @event.Contexts.Device.ApplyMauiDeviceData(_options.DiagnosticLogger,
                                                   _options.NetworkStatusListener,
                                                   _deviceIdiom,
                                                   _deviceInfo,
                                                   _displayInfo,
                                                   _deviceSupportsVibration,
                                                   _deviceSupportsAccelerometer,
                                                   _deviceSupportsGyroscope);
        @event.Contexts.OperatingSystem.ApplyMauiOsData(_options.DiagnosticLogger);
        @event.Contexts.App.InForeground = InForeground;

        return @event;
    }
}

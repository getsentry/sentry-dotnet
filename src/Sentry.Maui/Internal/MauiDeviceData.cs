using Sentry.Extensibility;
using Sentry.Protocol;
using Device = Sentry.Protocol.Device;

namespace Sentry.Maui.Internal;

internal static class MauiDeviceData
{
    public static void ApplyMauiDeviceData(this Device device, IDiagnosticLogger? logger, INetworkStatusListener? networkStatusListener, string deviceIdiom, IDeviceInfo deviceInfo, DisplayInfo displayInfo, bool supportsVibration, bool supportsAccelerometer, bool supportsGyroscope)
    {
        try
        {
            // TODO: Add more device data where indicated

            if (deviceInfo.Platform == DevicePlatform.Unknown)
            {
                // return early so we don't get NotImplementedExceptions (i.e., in unit tests, etc.)
                return;
            }
            device.Name ??= deviceInfo.Name;
            device.Manufacturer ??= deviceInfo.Manufacturer;
            device.Model ??= deviceInfo.Model;
            device.DeviceType ??= deviceIdiom;
            device.Simulator = deviceInfo.DeviceType switch
            {
                DeviceType.Virtual => true,
                DeviceType.Physical => false,
                _ => null
            };
            // device.Brand ??= ?
            // device.Family ??= ?
            // device.ModelId ??= ?
            device.Architecture ??= RuntimeInformation.OSArchitecture.ToString();
            // ? = deviceInfo.Platform;
            // ? = deviceInfo.VersionString;

            // https://docs.microsoft.com/dotnet/maui/platform-integration/device/battery
            try
            {
                var battery = Battery.Default;
                device.BatteryLevel ??= battery.ChargeLevel < 0 ? null : (float)(battery.ChargeLevel * 100.0);
                device.BatteryStatus ??= battery.State.ToString();
                device.IsCharging ??= battery.State switch
                {
                    BatteryState.Unknown => null,
                    BatteryState.Charging => true,
                    _ => false
                };
            }
            catch (PermissionException)
            {
                logger?.LogDebug("No permission to read battery state from the device.");
            }

            device.IsOnline ??= networkStatusListener!.Online;

#if MACCATALYST || IOS
            if (MainThread.IsMainThread)
            {
                CaptureDisplayInfo();
            }
            else
            {
                // On iOS and Mac Catalyst, Accessing DeviceDisplay.Current must be done on the UI thread or else an
                // exception will be thrown.
                // See https://learn.microsoft.com/en-us/dotnet/maui/platform-integration/device/display?view=net-maui-8.0&tabs=macios#platform-differences
                using var resetEvent = new ManualResetEventSlim(false);
                // ReSharper disable once AccessToDisposedClosure - not disposed until lambda completes
                MainThread.BeginInvokeOnMainThread(() => CaptureDisplayInfo(resetEvent));
                resetEvent.Wait();
            }
#else
            CaptureDisplayInfo(displayInfo);
#endif

            device.SupportsVibration ??= supportsVibration;
            device.SupportsAccelerometer ??= supportsAccelerometer;
            device.SupportsGyroscope ??= supportsGyroscope;

            // https://docs.microsoft.com/dotnet/maui/platform-integration/device/geolocation
            // TODO: How to get without actually trying to make a location request?
            // device.SupportsLocationService ??= Geolocation.Default.???

            // device.SupportsAudio ??= ?

            // device.MemorySize ??=
            // device.FreeMemory ??=
            // device.UsableMemory ??=
            // device.LowMemory ??=

            // device.StorageSize ??=
            // device.FreeStorage ??=
            // device.ExternalStorageSize ??=
            // device.ExternalFreeStorage ??=

            // device.BootTime ??=
            // device.DeviceUniqueIdentifier ??=

            //device.CpuDescription ??= ?
            //device.ProcessorCount ??= ?
            //device.ProcessorFrequency ??= ?

        }
        catch (Exception ex)
        {
            // Log, but swallow the exception so we can continue sending events
            logger?.LogError(ex, "Error getting MAUI device information.");
        }

#if MACCATALYST || IOS
        void CaptureDisplayInfo(ManualResetEventSlim? resetEvent = null)
        {
            // https://docs.microsoft.com/dotnet/maui/platform-integration/device/display
            var display = DeviceDisplay.MainDisplayInfo;
            device.ScreenResolution ??= $"{(int)display.Width}x{(int)display.Height}";
            device.ScreenDensity ??= (float)display.Density;
            device.Orientation ??= display.Orientation switch
            {
                DisplayOrientation.Portrait => DeviceOrientation.Portrait,
                DisplayOrientation.Landscape => DeviceOrientation.Landscape,
                _ => null
            };
            // device.ScreenDpi ??= ?
            // ? = display.RefreshRate;
            // ? = display.Rotation;
            resetEvent?.Set();
        }
#else

        void CaptureDisplayInfo(DisplayInfo display)
        {
            device.ScreenResolution ??= $"{(int)display.Width}x{(int)display.Height}";
            device.ScreenDensity ??= (float)display.Density;
            device.Orientation ??= display.Orientation switch
            {
                DisplayOrientation.Portrait => DeviceOrientation.Portrait,
                DisplayOrientation.Landscape => DeviceOrientation.Landscape,
                _ => null
            };
            // device.ScreenDpi ??= ?
            // ? = display.RefreshRate;
            // ? = display.Rotation;
        }
#endif
    }
}

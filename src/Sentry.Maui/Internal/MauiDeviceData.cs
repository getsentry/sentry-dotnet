using Sentry.Extensibility;
using Sentry.Protocol;
using Device = Sentry.Protocol.Device;

namespace Sentry.Maui.Internal;

internal static class MauiDeviceData
{
    public static void ApplyMauiDeviceData(this Device device, IDiagnosticLogger? logger)
    {
        try
        {
            // TODO: Add more device data where indicated

            // https://docs.microsoft.com/dotnet/maui/platform-integration/device/information
            var deviceInfo = DeviceInfo.Current;
            if (deviceInfo.Platform == DevicePlatform.Unknown)
            {
                // return early so we don't get NotImplementedExceptions (i.e., in unit tests, etc.)
                return;
            }
            device.Name ??= deviceInfo.Name;
            device.Manufacturer ??= deviceInfo.Manufacturer;
            device.Model ??= deviceInfo.Model;
            device.DeviceType ??= deviceInfo.Idiom.ToString();
            device.Simulator ??= deviceInfo.DeviceType switch
            {
                DeviceType.Virtual => true,
                DeviceType.Physical => false,
                _ => null
            };
            // device.Brand ??= ?
            // device.Family ??= ?
            // device.ModelId ??= ?
            // device.Architecture ??= ?
            // ? = deviceInfo.Platform;
            // ? = deviceInfo.VersionString;

            // https://docs.microsoft.com/dotnet/maui/platform-integration/device/battery
            try
            {
                var battery = Battery.Default;
                device.BatteryLevel ??= battery.ChargeLevel < 0 ? null : (short)battery.ChargeLevel;
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

            // https://docs.microsoft.com/dotnet/maui/platform-integration/communication/networking#using-connectivity
            try
            {
                device.IsOnline ??= Connectivity.NetworkAccess == NetworkAccess.Internet;
            }
            catch (PermissionException)
            {
                logger?.LogDebug("No permission to read network state from the device.");
            }

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

            // https://docs.microsoft.com/dotnet/maui/platform-integration/device/vibrate
            device.SupportsVibration ??= Vibration.Default.IsSupported;

            // https://docs.microsoft.com/dotnet/maui/platform-integration/device/sensors
            device.SupportsAccelerometer ??= Accelerometer.IsSupported;
            device.SupportsGyroscope ??= Gyroscope.IsSupported;

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
            logger?.LogError("Error getting MAUI device information.", ex);
        }
    }
}

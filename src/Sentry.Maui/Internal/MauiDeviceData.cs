using Sentry.Extensibility;
using Sentry.Protocol;
using Device = Sentry.Protocol.Device;

namespace Sentry.Maui.Internal;

internal static class MauiDeviceData
{
#if ANDROID
    private static readonly Lock JniLock = new();
#endif

    public static void ApplyMauiDeviceData(this Device device, IDiagnosticLogger? logger,
        INetworkStatusListener? networkStatusListener)
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
#if ANDROID
            // DeviceInfo.Idiom is not threadsafe on Android
            // See: https://github.com/getsentry/sentry-dotnet/issues/3627
            lock (JniLock)
            {
                device.DeviceType ??= deviceInfo.Idiom.ToString();
            }
#else
            device.DeviceType ??= deviceInfo.Idiom.ToString();
#endif

            device.Simulator ??= deviceInfo.DeviceType switch
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

            try
            {
                // Note: Connectivity.NetworkAccess is not threadsafe on Android. As we already have a network listener
                // monitoring the status of the network, we get the online satus from there instead (on all platforms)
                // See: https://github.com/getsentry/sentry-dotnet/issues/3627
                device.IsOnline ??= networkStatusListener?.Online;
            }
            catch (PermissionException)
            {
                logger?.LogDebug("No permission to read network state from the device.");
            }

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
#elif ANDROID
            device.ApplyAndroidDisplayInfo();
#else
            CaptureDisplayInfo();
#endif

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
            logger?.LogError(ex, "Error getting MAUI device information.");
        }

#if !ANDROID
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
#endif
    }

#if ANDROID
    // MAUI's DeviceDisplay can abort the process when read concurrently: https://github.com/dotnet/maui/issues/40510
    private static void ApplyAndroidDisplayInfo(this Device device)
    {
        var resources = global::Android.App.Application.Context.Resources;
        if (resources?.DisplayMetrics is not { } metrics)
        {
            return;
        }

        device.ScreenResolution ??= $"{metrics.WidthPixels}x{metrics.HeightPixels}";
        device.ScreenDensity ??= metrics.Density;
        device.ScreenDpi ??= (int)metrics.DensityDpi;
        device.Orientation ??= resources.Configuration?.Orientation switch
        {
            global::Android.Content.Res.Orientation.Portrait => DeviceOrientation.Portrait,
            global::Android.Content.Res.Orientation.Landscape => DeviceOrientation.Landscape,
            _ => null
        };
    }
#endif
}

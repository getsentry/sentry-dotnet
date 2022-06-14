using Sentry.Protocol;

namespace Sentry.Android.Extensions;

internal static class DeviceExtensions
{
    public static void ApplyTo(this Java.Protocol.Device d, Device device)
    {
        device.Name = d.Name;
        device.Manufacturer = d.Manufacturer;
        device.Brand = d.Brand;
        device.Family = d.Family;
        device.Model = d.Model;
        device.ModelId = d.ModelId;
        device.Architecture = d.GetArchs()?.FirstOrDefault();
        device.BatteryLevel = d.BatteryLevel?.ShortValue();
        device.IsCharging = d.IsCharging()?.BooleanValue();
        device.IsOnline = d.IsOnline()?.BooleanValue();
        device.Orientation = d.Orientation?.ToDeviceOrientation();
        device.Simulator = d.IsSimulator()?.BooleanValue();
        device.MemorySize = d.MemorySize?.LongValue();
        device.FreeMemory = d.FreeMemory?.LongValue();
        device.UsableMemory = d.UsableMemory?.LongValue();
        device.LowMemory = d.IsLowMemory()?.BooleanValue();
        device.StorageSize = d.StorageSize?.LongValue();
        device.FreeStorage = d.FreeStorage?.LongValue();
        device.ExternalStorageSize = d.ExternalStorageSize?.LongValue();
        device.ExternalFreeStorage = d.ExternalFreeStorage?.LongValue();
        device.ScreenResolution = $"{d.ScreenWidthPixels}x{d.ScreenHeightPixels}";
        device.ScreenDensity = d.ScreenDensity?.FloatValue();
        device.ScreenDpi = d.ScreenDpi?.IntValue();
        device.BootTime = d.BootTime?.ToDateTimeOffset();
        device.DeviceUniqueIdentifier = d.Id;

        // TODO: Can we get these from somewhere?
        //device.ProcessorCount = ?
        //device.CpuDescription = ?
        //device.ProcessorFrequency = ?
        //device.DeviceType = ?
        //device.BatteryStatus = ?
        //device.SupportsVibration = ?
        //device.SupportsAccelerometer = ?
        //device.SupportsGyroscope = ?
        //device.SupportsAudio = ?
        //device.SupportsLocationService = ?

    }

    public static DeviceOrientation ToDeviceOrientation(this Java.Protocol.Device.DeviceOrientation orientation) =>
        orientation.Name() switch
        {
            "PORTRAIT" => DeviceOrientation.Portrait,
            "LANDSCAPE" => DeviceOrientation.Landscape,
            _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation.Name(), message: default)
        };
}

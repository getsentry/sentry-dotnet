using Sentry.Extensibility;
using OperatingSystem = Sentry.Protocol.OperatingSystem;

namespace Sentry.Maui.Internal;

internal static class MauiLogAttributes
{
    public static IReadOnlyList<KeyValuePair<string, string>> Create(IDiagnosticLogger? logger) =>
        Create(DeviceInfo.Current, logger);

    internal static IReadOnlyList<KeyValuePair<string, string>> Create(IDeviceInfo deviceInfo, IDiagnosticLogger? logger)
    {
        var attributes = new List<KeyValuePair<string, string>>();
        try
        {
            if (deviceInfo.Platform == DevicePlatform.Unknown)
            {
                return attributes;
            }

            var os = new OperatingSystem();
            os.ApplyMauiOsData(deviceInfo, logger);
            Add(attributes, "os.name", os.Name);
            Add(attributes, "os.version", os.Version);
#if ANDROID
            Add(attributes, "device.brand", global::Android.OS.Build.Brand);
#else
            Add(attributes, "device.brand", deviceInfo.Manufacturer);
#endif
            Add(attributes, "device.model", deviceInfo.Model);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting MAUI device attributes for logs.");
        }
        return attributes;
    }

    private static void Add(List<KeyValuePair<string, string>> attributes, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            attributes.Add(new KeyValuePair<string, string>(key, value));
        }
    }
}

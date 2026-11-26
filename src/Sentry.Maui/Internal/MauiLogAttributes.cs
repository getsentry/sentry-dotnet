using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;
using Device = Sentry.Protocol.Device;
using OperatingSystem = Sentry.Protocol.OperatingSystem;

namespace Sentry.Maui.Internal;

internal static class MauiLogAttributes
{
    public static IReadOnlyList<KeyValuePair<string, string>> Create(IDiagnosticLogger? logger) =>
        Create(DeviceInfo.Current, logger);

    internal static IReadOnlyList<KeyValuePair<string, string>> Create(IDeviceInfo deviceInfo, IDiagnosticLogger? logger)
    {
        try
        {
            if (deviceInfo.Platform == DevicePlatform.Unknown)
            {
                return [];
            }

            var os = new OperatingSystem();
            os.ApplyMauiOsData(deviceInfo, logger);
            var device = new Device { Brand = deviceInfo.Manufacturer, Model = deviceInfo.Model };
            return DeviceLogAttributes.Create(os, device);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting MAUI device attributes for logs.");
            return [];
        }
    }
}

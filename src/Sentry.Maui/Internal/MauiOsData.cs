using Sentry.Extensibility;
using OperatingSystem = Sentry.Protocol.OperatingSystem;

namespace Sentry.Maui.Internal;

internal static class MauiOsData
{
    public static void ApplyMauiOsData(this OperatingSystem os, IDiagnosticLogger? logger)
    {
        try
        {
            // https://docs.microsoft.com/dotnet/maui/platform-integration/device/information
            var deviceInfo = DeviceInfo.Current;
            if (deviceInfo.Platform == DevicePlatform.Unknown)
            {
                // return early so we don't get NotImplementedExceptions (i.e., in unit tests, etc.)
                return;
            }

            os.Name ??= deviceInfo.Platform.ToString();
            os.Version ??= deviceInfo.VersionString;

            // TODO: fill in these
            // os.Build ??= ?
            // os.KernelVersion ??= ?
            // os.Rooted ??= ?
        }
        catch (Exception ex)
        {
            // Log, but swallow the exception so we can continue sending events
            logger?.LogError("Error getting MAUI device information.", ex);
        }
    }
}

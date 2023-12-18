using Microsoft.Win32;
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

            os.Version = deviceInfo.VersionString;

#if WINDOWS
            os.Name ??= "Windows";

            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key?.GetValue("DisplayVersion") is string displayVersion)
            {
                os.Build = displayVersion;
            }
            else if (key?.GetValue("ReleaseId") is string releaseId)
            {
                os.Build = releaseId;
            }
#else
            os.Name = deviceInfo.Platform.ToString();
#endif

        }
        catch (Exception ex)
        {
            // Log, but swallow the exception so we can continue sending events
            logger?.LogError(ex, "Error getting MAUI OS information.");
        }
    }
}

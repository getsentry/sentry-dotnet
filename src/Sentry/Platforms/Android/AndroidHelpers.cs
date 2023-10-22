using Sentry.Android.AssemblyReader;
using Sentry.Extensibility;

namespace Sentry.Android;

internal static class AndroidHelpers
{
    public static string? GetCpuAbi() => GetSupportedAbis().FirstOrDefault();

    public static IList<string> GetSupportedAbis()
    {
        var result = AndroidBuild.SupportedAbis;
        if (result != null)
        {
            return result;
        }

#pragma warning disable CS0618
        var abi = AndroidBuild.CpuAbi;
#pragma warning restore CS0618

        return abi != null ? new[] {abi} : Array.Empty<string>();
    }

    public static IAndroidAssemblyReader? GetAndroidAssemblyReader(IDiagnosticLogger? logger)
    {
        var apkPath = Application.Context.ApplicationInfo?.SourceDir;
        if (apkPath == null)
        {
            logger?.LogWarning("Can't determine APK path.");
            return null;
        }

        if (!File.Exists(apkPath))
        {
            logger?.LogWarning("APK doesn't exist at {0}", apkPath);
            return null;
        }

        try
        {
            var supportedAbis = GetSupportedAbis();
            return AndroidAssemblyReaderFactory.Open(apkPath, supportedAbis,
                logger: (message, args) => logger?.Log(SentryLevel.Debug, message, args: args));
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Cannot create assembly reader.");
            return null;
        }
    }
}

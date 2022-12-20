using Sentry.Extensibility;

namespace Sentry.Android.AssemblyReader;

internal static class AndroidAssemblyReaderFactory
{
    public static IAndroidAssemblyReader Open(string apkPath, IList<string> supportedAbis, IDiagnosticLogger? logger)
    {
        logger?.LogDebug("Opening APK: {0}", apkPath);
        var zipArchive = ZipFile.Open(apkPath, ZipArchiveMode.Read);

        if (zipArchive.GetEntry("assemblies/assemblies.manifest") is not null)
        {
            logger?.LogDebug("APK uses AssemblyStore");
            return new AndroidAssemblyStoreReader(zipArchive, supportedAbis, logger);
        }

        logger?.LogDebug("APK doesn't use AssemblyStore");
        return new AndroidAssemblyDirectoryReader(zipArchive, supportedAbis, logger);
    }
}

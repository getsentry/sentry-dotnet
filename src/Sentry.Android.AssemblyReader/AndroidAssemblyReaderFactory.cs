using Sentry.Android.AssemblyReader.V1;

namespace Sentry.Android.AssemblyReader;

/// <summary>
/// A factory that creates an assembly reader for an Android APK.
/// </summary>
public static class AndroidAssemblyReaderFactory
{
    /// <summary>
    /// Opens an assembly reader.
    /// </summary>
    /// <param name="apkPath">The path to the APK</param>
    /// <param name="supportedAbis">The supported ABIs</param>
    /// <param name="logger">An optional logger for debugging</param>
    /// <returns>The reader</returns>
    public static IAndroidAssemblyReader Open(string apkPath, IList<string> supportedAbis, DebugLogger? logger = null)
    {
        logger?.Invoke("Opening APK: {0}", apkPath);
        var zipArchive = ZipFile.Open(apkPath, ZipArchiveMode.Read);

        // TODO: Try to read using the v2 store format

        // Try to read using the v1 store format
        if (zipArchive.GetEntry("assemblies/assemblies.manifest") is not null)
        {
            logger?.Invoke("APK uses AssemblyStore");
            return new AndroidAssemblyStoreReader(zipArchive, supportedAbis, logger);
        }

        // Try to read from file system
        logger?.Invoke("APK doesn't use AssemblyStore");
        return new AndroidAssemblyDirectoryReader(zipArchive, supportedAbis, logger);
    }
}

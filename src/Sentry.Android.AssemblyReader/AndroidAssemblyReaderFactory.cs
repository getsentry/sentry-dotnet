using Sentry.Android.AssemblyReader.V1;
using Sentry.Android.AssemblyReader.V2;

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

        // Try to read using the v2 store format
        if (AndroidAssemblyStoreReaderV2.TryReadStore(apkPath, supportedAbis, logger, out var readerV2))
        {
            logger?.Invoke("APK uses AssemblyStore V2");
            return readerV2;
        }

        // Try to read using the v1 store format
        var zipArchive = ZipFile.Open(apkPath, ZipArchiveMode.Read);
        if (zipArchive.GetEntry("assemblies/assemblies.manifest") is not null)
        {
            logger?.Invoke("APK uses AssemblyStore V1");
            return new AndroidAssemblyStoreReaderV1(zipArchive, supportedAbis, logger);
        }

        // Finally, try to read from file system
        logger?.Invoke("APK doesn't use AssemblyStore");
        return new AndroidAssemblyDirectoryReader(zipArchive, supportedAbis, logger);
    }
}

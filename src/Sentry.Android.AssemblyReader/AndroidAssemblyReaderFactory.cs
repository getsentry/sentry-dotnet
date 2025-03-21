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

#if NET9_0
        logger?.Invoke("Reading files using V2 APK layout.");
        if (AndroidAssemblyStoreReaderV2.TryReadStore(apkPath, supportedAbis, logger, out var readerV2))
        {
            logger?.Invoke("APK uses AssemblyStore V2");
            return readerV2;
        }

        logger?.Invoke("APK doesn't use AssemblyStore");
        return new AndroidAssemblyDirectoryReaderV2(apkPath, supportedAbis, logger);
#else
        logger?.Invoke("Reading files using V1 APK layout.");

        var zipArchive = ZipFile.OpenRead(apkPath);
        if (zipArchive.GetEntry("assemblies/assemblies.manifest") is not null)
        {
            logger?.Invoke("APK uses AssemblyStore V1");
            return new AndroidAssemblyStoreReaderV1(zipArchive, supportedAbis, logger);
        }

        logger?.Invoke("APK doesn't use AssemblyStore");
        return new AndroidAssemblyDirectoryReaderV1(zipArchive, supportedAbis, logger);
#endif
    }
}

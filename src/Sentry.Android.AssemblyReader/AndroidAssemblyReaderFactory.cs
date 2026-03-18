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
        logger?.Invoke(DebugLoggerLevel.Debug, "Opening APK: {0}", apkPath);

        if (AndroidAssemblyStoreReader.TryReadStore(apkPath, supportedAbis, logger, out var readerV2))
        {
            logger?.Invoke(DebugLoggerLevel.Debug, "APK uses AssemblyStore");
            return readerV2;
        }

        logger?.Invoke(DebugLoggerLevel.Debug, "APK doesn't use AssemblyStore");
        return new AndroidAssemblyDirectoryReader(apkPath, supportedAbis, logger);
    }
}

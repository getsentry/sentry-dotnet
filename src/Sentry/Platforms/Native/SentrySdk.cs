using Sentry.Extensibility;
using Sentry.Native;

// ReSharper disable once CheckNamespace
namespace Sentry;

public static partial class SentrySdk
{
    private static readonly Dictionary<string, bool> PerDirectoryCrashInfo = new();

    private static void InitNativeSdk(SentryOptions options)
    {
        if (!C.Init(options))
        {
            options.DiagnosticLogger?
                .LogWarning("Sentry native initialization failed - native crashes are not captured.");
            return;
        }

        // Setup future scope updates
        options.ScopeObserver = new NativeScopeObserver(options);
        options.EnableScopeSync = true;

        // Trigger an initial scope sync
        options.NativeContextWriter = new NativeContextWriter();

        // Note: we must actually call the function now and on every other call use the value we get here.
        // Additionally, we cannot call this multiple times for the same directory, because the result changes
        // on subsequent runs. Therefore, we cache the value during the whole runtime of the application.
        var cacheDirectory = C.GetCacheDirectory(options);
        var crashedLastRun = false;
        // In the event the SDK is re-initialized with a different path on disk, we need to track which ones were already read
        // Similarly we need to cache the value of each call since a subsequent call would return a different value
        // as the file used on disk to mark it as crashed is deleted after we read it.
        lock (PerDirectoryCrashInfo)
        {
            if (!PerDirectoryCrashInfo.TryGetValue(cacheDirectory, out crashedLastRun))
            {
                crashedLastRun = C.HandleCrashedLastRun(options);
                PerDirectoryCrashInfo.Add(cacheDirectory, crashedLastRun);

                options.DiagnosticLogger?
                    .LogDebug("Native SDK reported: 'crashedLastRun': '{0}'", crashedLastRun);
            }
        }
        options.CrashedLastRun = () => crashedLastRun;
    }

    private static void CloseNativeSdk() => C.Close();


}

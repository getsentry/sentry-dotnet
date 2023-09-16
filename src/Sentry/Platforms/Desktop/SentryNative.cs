using Sentry.Extensibility;

namespace Sentry.Desktop;

// https://github.com/getsentry/sentry-unity/blob/3eb6eca6ed270c5ec023bf75ee53c1ca00bb7c82/src/Sentry.Unity.Native/SentryNative.cs

/// <summary>
/// Access to the Sentry native support of the sentry-native SDK.
/// </summary>
internal static class SentryNative
{
    private static readonly Dictionary<string, bool> PerDirectoryCrashInfo = new();

    /// <summary>
    /// Configures the native SDK.
    /// </summary>
    /// <param name="options">The Sentry Unity options to use.</param>
    public static void Configure(SentryOptions options)
    {
        if (!SentryNativeBridge.Init(options))
        {
            options.DiagnosticLogger?
                .LogWarning("Sentry native initialization failed - native crashes are not captured.");
            return;
        }

        // ApplicationAdapter.Instance.Quitting += () =>
        // {
        //     options.DiagnosticLogger?.LogDebug("Closing the sentry-native SDK");
        //     SentryNativeBridge.Close();
        // };
        options.ScopeObserver = new NativeScopeObserver(options);
        options.EnableScopeSync = true;

        // options.NativeContextWriter = new NativeContextWriter();

        // // Use AnalyticsSessionInfo.userId as the default UserID in native & dotnet
        // options.DefaultUserId = AnalyticsSessionInfo.userId;
        // if (options.DefaultUserId is not null)
        // {
        //     options.ScopeObserver.SetUser(new User { Id = options.DefaultUserId });
        // }

        // Note: we must actually call the function now and on every other call use the value we get here.
        // Additionally, we cannot call this multiple times for the same directory, because the result changes
        // on subsequent runs. Therefore, we cache the value during the whole runtime of the application.
        var cacheDirectory = SentryNativeBridge.GetCacheDirectory(options);
        var crashedLastRun = false;
        // In the event the SDK is re-initialized with a different path on disk, we need to track which ones were already read
        // Similarly we need to cache the value of each call since a subsequent call would return a different value
        // as the file used on disk to mark it as crashed is deleted after we read it.
        lock (PerDirectoryCrashInfo)
        {
            if (!PerDirectoryCrashInfo.TryGetValue(cacheDirectory, out crashedLastRun))
            {
                crashedLastRun = SentryNativeBridge.HandleCrashedLastRun(options);
                PerDirectoryCrashInfo.Add(cacheDirectory, crashedLastRun);

                options.DiagnosticLogger?
                    .LogDebug("Native SDK reported: 'crashedLastRun': '{0}'", crashedLastRun);
            }
        }
        options.CrashedLastRun = () => crashedLastRun;
    }
}

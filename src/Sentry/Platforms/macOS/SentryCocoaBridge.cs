using Sentry.Extensibility;

// https://github.com/getsentry/sentry-unity/blob/3eb6eca6ed270c5ec023bf75ee53c1ca00bb7c82/src/Sentry.Unity.iOS/SentryNativeCocoa.cs

namespace Sentry.macOS;

    /// <summary>
    /// Access to the Sentry native support on iOS/macOS.
    /// </summary>
    internal static class SentryCocoaBridge
    {
        internal static void Configure(SentryOptions options)
            // , ISentryUnityInfo sentryUnityInfo, RuntimePlatform platform)
        {
            if (!options.EnableNativeCrashReporting)
            {
                options.DiagnosticLogger?.LogDebug("Not initializing Sentry Cocoa. EnableNativeCrashReporting is false.");
                return;
            }
            if (!SentryCocoaBridgeProxy.Init(options))
            {
                options.DiagnosticLogger?.LogError("Failed to initialize the native SDK. This doesn't affect .NET monitoring.");
                return;
            }

            // options.NativeContextWriter = new CocoaContextWriter();
            options.ScopeObserver = new CocoaScopeObserver("macOS", options);
            options.EnableScopeSync = true;
            options.CrashedLastRun = () =>
            {
                var crashedLastRun = SentryCocoaBridgeProxy.CrashedLastRun() == 1;
                options.DiagnosticLogger?
                    .LogDebug("Native SDK reported: 'crashedLastRun': '{0}'", crashedLastRun);

                return crashedLastRun;
            };

            // options.NativeSupportCloseCallback += () => Close(options.DiagnosticLogger);
            // if (sentryUnityInfo.IL2CPP)
            // {
            //     options.DefaultUserId = SentryCocoaBridgeProxy.GetInstallationId();
            // }
        }

        /// <summary>
        /// Closes the native Cocoa support.
        /// </summary>
        public static void Close(IDiagnosticLogger? logger = null)
        {
            logger?.LogDebug("Closing the sentry-cocoa SDK");
            SentryCocoaBridgeProxy.Close();
        }
    }

using Sentry.Extensibility;

namespace Sentry.macOS;

// https://github.com/getsentry/sentry-unity/blob/3eb6eca6ed270c5ec023bf75ee53c1ca00bb7c82/src/Sentry.Unity.iOS/SentryCocoaBridgeProxy.cs

/// <summary>
/// P/Invoke to SentryNativeBridge.m which communicates with the `sentry-cocoa` SDK.
/// </summary>
/// <remarks>
/// Functions are declared in `SentryNativeBridge.m`
/// </remarks>
/// <see href="https://github.com/getsentry/sentry-cocoa"/>
internal static class SentryCocoaBridgeProxy
{
    // Note: used on macOS only
    public static bool Init(SentryOptions options)
    {
        if (LoadLibrary() != 1)
        {
            return false;
        }

        var cOptions = OptionsNew();

        // Note: DSN is not null because options.IsValid() must have returned true for this to be called.
        OptionsSetString(cOptions, "dsn", options.Dsn!);

        if (options.Release is not null)
        {
            options.DiagnosticLogger?.LogDebug("Setting Release: {0}", options.Release);
            OptionsSetString(cOptions, "release", options.Release);
        }

        if (options.Environment is not null)
        {
            options.DiagnosticLogger?.LogDebug("Setting Environment: {0}", options.Environment);
            OptionsSetString(cOptions, "environment", options.Environment);
        }

        options.DiagnosticLogger?.LogDebug("Setting Debug: {0}", options.Debug);
        OptionsSetInt(cOptions, "debug", options.Debug ? 1 : 0);

        var diagnosticLevel = options.DiagnosticLevel.ToString().ToLowerInvariant();
        options.DiagnosticLogger?.LogDebug("Setting DiagnosticLevel: {0}", diagnosticLevel);
        OptionsSetString(cOptions, "diagnosticLevel", diagnosticLevel);

        options.DiagnosticLogger?.LogDebug("Setting SendDefaultPii: {0}", options.SendDefaultPii);
        OptionsSetInt(cOptions, "sendDefaultPii", options.SendDefaultPii ? 1 : 0);

        // macOS screenshots currently don't work, because there's no UIKit. Cocoa logs: "Sentry - info:: NO UIKit"
        // options.DiagnosticLogger?.LogDebug("Setting AttachScreenshot: {0}", options.AttachScreenshot);
        // OptionsSetInt(cOptions, "attachScreenshot", options.AttachScreenshot ? 1 : 0);
        OptionsSetInt(cOptions, "attachScreenshot", 0);

        options.DiagnosticLogger?.LogDebug("Setting MaxBreadcrumbs: {0}", options.MaxBreadcrumbs);
        OptionsSetInt(cOptions, "maxBreadcrumbs", options.MaxBreadcrumbs);

        options.DiagnosticLogger?.LogDebug("Setting MaxCacheItems: {0}", options.MaxCacheItems);
        OptionsSetInt(cOptions, "maxCacheItems", options.MaxCacheItems);

        StartWithOptions(cOptions);
        return true;
    }

    [DllImport("libBridge", EntryPoint = "SentryNativeBridgeLoadLibrary")]
    private static extern int LoadLibrary();

    [DllImport("libBridge", EntryPoint = "SentryNativeBridgeOptionsNew")]
    private static extern IntPtr OptionsNew();

    [DllImport("libBridge", EntryPoint = "SentryNativeBridgeOptionsSetString")]
    private static extern void OptionsSetString(IntPtr options, string name, string value);

    [DllImport("libBridge", EntryPoint = "SentryNativeBridgeOptionsSetInt")]
    private static extern void OptionsSetInt(IntPtr options, string name, int value);

    [DllImport("libBridge", EntryPoint = "SentryNativeBridgeStartWithOptions")]
    private static extern void StartWithOptions(IntPtr options);

    [DllImport("libBridge", EntryPoint = "SentryNativeBridgeCrashedLastRun")]
    public static extern int CrashedLastRun();

    [DllImport("libBridge", EntryPoint = "SentryNativeBridgeClose")]
    public static extern void Close();

    [DllImport("libBridge")]
    public static extern void SentryNativeBridgeAddBreadcrumb(string timestamp, string? message, string? type, string? category, int level);

    [DllImport("libBridge")]
    public static extern void SentryNativeBridgeSetExtra(string key, string? value);

    [DllImport("libBridge")]
    public static extern void SentryNativeBridgeSetTag(string key, string value);

    [DllImport("libBridge")]
    public static extern void SentryNativeBridgeUnsetTag(string key);

    [DllImport("libBridge")]
    public static extern void SentryNativeBridgeSetUser(string? email, string? userId, string? ipAddress, string? username);

    [DllImport("libBridge")]
    public static extern void SentryNativeBridgeUnsetUser();

    [DllImport("libBridge", EntryPoint = "SentryNativeBridgeGetInstallationId")]
    public static extern string GetInstallationId();

    [DllImport("libBridge", EntryPoint = "crashBridge")]
    public static extern void Crash();
}

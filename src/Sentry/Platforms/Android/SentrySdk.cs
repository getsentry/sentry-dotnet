using Android.Content.PM;
using Android.OS;
using Sentry.Android;
using Sentry.Android.Callbacks;
using Sentry.Android.Extensions;
using Sentry.Protocol;

// ReSharper disable once CheckNamespace
namespace Sentry;

public static partial class SentrySdk
{
    private static AndroidContext AppContext { get; set; } = Application.Context;

    /// <summary>
    /// Initializes the SDK for Android, with an optional configuration options callback.
    /// </summary>
    /// <param name="context">The Android application context.</param>
    /// <param name="configureOptions">The configuration options callback.</param>
    /// <returns>An object that should be disposed when the application terminates.</returns>
    [Obsolete("It is no longer required to provide the application context when calling Init. " +
              "This method may be removed in a future major release.")]
    public static IDisposable Init(AndroidContext context, Action<SentryOptions>? configureOptions)
    {
        AppContext = context;
        return Init(configureOptions);
    }

    /// <summary>
    /// Initializes the SDK for Android, using a configuration options instance.
    /// </summary>
    /// <param name="context">The Android application context.</param>
    /// <param name="options">The configuration options instance.</param>
    /// <returns>An object that should be disposed when the application terminates.</returns>
    [Obsolete("It is no longer required to provide the application context when calling Init. " +
              "This method may be removed in a future major release.")]
    public static IDisposable Init(AndroidContext context, SentryOptions options)
    {
        AppContext = context;
        return Init(options);
    }

    private static void InitSentryAndroidSdk(SentryOptions options)
    {
        // Set default release and distribution
        options.Release ??= GetDefaultReleaseString();
        options.Distribution ??= GetDefaultDistributionString();

        // Make sure we capture managed exceptions from the Android environment
        AndroidEnvironment.UnhandledExceptionRaiser += AndroidEnvironment_UnhandledExceptionRaiser;

        // Now initialize the Android SDK
        SentryAndroidOptions? androidOptions = null;
        SentryAndroid.Init(AppContext, new JavaLogger(options),
            new OptionsConfigurationCallback(o =>
            {
                // Capture the android options reference on the outer scope
                androidOptions = o;

                // TODO: Should we set the DistinctId to match the one used by GlobalSessionManager?
                //o.DistinctId = ?

                // These options are copied over from our SentryOptions
                o.AttachStacktrace = options.AttachStacktrace;
                o.Debug = options.Debug;
                o.DiagnosticLevel = options.DiagnosticLevel.ToJavaSentryLevel();
                o.Dist = options.Distribution;
                o.Dsn = options.Dsn;
                o.EnableAutoSessionTracking = options.AutoSessionTracking;
                o.Environment = options.Environment;
                o.FlushTimeoutMillis = (long)options.InitCacheFlushTimeout.TotalMilliseconds;
                o.MaxAttachmentSize = options.MaxAttachmentSize;
                o.MaxBreadcrumbs = options.MaxBreadcrumbs;
                o.MaxCacheItems = options.MaxCacheItems;
                o.MaxQueueSize = options.MaxQueueItems;
                o.Release = options.Release;
                o.SampleRate = (JavaDouble?)options.SampleRate;
                o.SendClientReports = options.SendClientReports;
                o.SendDefaultPii = options.SendDefaultPii;
                o.ServerName = options.ServerName;
                o.SessionTrackingIntervalMillis = (long)options.AutoSessionTrackingInterval.TotalMilliseconds;
                o.ShutdownTimeoutMillis = (long)options.ShutdownTimeout.TotalMilliseconds;

                if (options.CacheDirectoryPath is { } cacheDirectoryPath)
                {
                    // Set a separate cache path for the Android SDK so we don't step on the managed one
                    o.CacheDirPath = Path.Combine(cacheDirectoryPath, "android");
                }

                // NOTE: Tags in options.DefaultTags should not be passed down, because we already call SetTag on each
                //       one when sending events, which is relayed through the scope observer.

                if (options.HttpProxy is System.Net.WebProxy proxy)
                {
                    var creds = proxy.Credentials as System.Net.NetworkCredential;
                    o.SetProxy(new Java.SentryOptions.Proxy
                    {
                        Host = proxy.Address?.Host,
                        Port = proxy.Address?.Port.ToString(CultureInfo.InvariantCulture),
                        User = creds?.UserName,
                        Pass = creds?.Password
                    });
                }

                if (options.BeforeBreadcrumb is { } beforeBreadcrumb)
                {
                    o.BeforeBreadcrumb = new BeforeBreadcrumbCallback(beforeBreadcrumb);
                }

                // These options we have behind feature flags
                if (options.Android.EnableAndroidSdkTracing)
                {
                    o.TracesSampleRate = (JavaDouble?)options.TracesSampleRate;

                    if (options.TracesSampler is { } tracesSampler)
                    {
                        o.TracesSampler = new TracesSamplerCallback(tracesSampler);
                    }
                }

                if (options.Android.EnableAndroidSdkBeforeSend && options.BeforeSend is { } beforeSend)
                {
                    o.BeforeSend = new BeforeSendCallback(beforeSend, options, o);
                }

                // These options are from SentryAndroidOptions
                o.AttachScreenshot = options.Android.AttachScreenshot;
                o.AnrEnabled = options.Android.AnrEnabled;
                o.AnrReportInDebug = options.Android.AnrReportInDebug;
                o.AnrTimeoutIntervalMillis = (long)options.Android.AnrTimeoutInterval.TotalMilliseconds;
                o.EnableActivityLifecycleBreadcrumbs = options.Android.EnableActivityLifecycleBreadcrumbs;
                o.EnableAutoActivityLifecycleTracing = options.Android.EnableAutoActivityLifecycleTracing;
                o.EnableActivityLifecycleTracingAutoFinish = options.Android.EnableActivityLifecycleTracingAutoFinish;
                o.EnableAppComponentBreadcrumbs = options.Android.EnableAppComponentBreadcrumbs;
                o.EnableAppLifecycleBreadcrumbs = options.Android.EnableAppLifecycleBreadcrumbs;
                o.EnableSystemEventBreadcrumbs = options.Android.EnableSystemEventBreadcrumbs;
                o.EnableUserInteractionBreadcrumbs = options.Android.EnableUserInteractionBreadcrumbs;
                o.EnableUserInteractionTracing = options.Android.EnableUserInteractionTracing;

                // These options are in Java.SentryOptions but not ours
                o.AttachThreads = options.Android.AttachThreads;
                o.ConnectionTimeoutMillis = (int)options.Android.ConnectionTimeout.TotalMilliseconds;
                o.EnableNdk = options.Android.EnableNdk;
                o.EnableShutdownHook = options.Android.EnableShutdownHook;
                o.EnableUncaughtExceptionHandler = options.Android.EnableUncaughtExceptionHandler;
                o.ProfilesSampleRate = (JavaDouble?)options.Android.ProfilesSampleRate;
                o.PrintUncaughtStackTrace = options.Android.PrintUncaughtStackTrace;
                o.ReadTimeoutMillis = (int)options.Android.ReadTimeout.TotalMilliseconds;

                // In-App Excludes and Includes to be passed to the Android SDK
                options.Android.InAppExcludes?.ForEach(x => o.AddInAppExclude(x));
                options.Android.InAppIncludes?.ForEach(x => o.AddInAppInclude(x));

                // These options are intentionally set and not exposed for modification
                o.EnableExternalConfiguration = false;
                o.EnableDeduplication = false;
                o.AttachServerName = false;

                // These options are intentionally not expose or modified
                //o.MaxRequestBodySize   // N/A for Android apps
                //o.MaxSpans             // See https://github.com/getsentry/sentry-dotnet/discussions/1698

                // Don't capture managed exceptions in the native SDK, since we already capture them in the managed SDK
                o.AddIgnoredExceptionForType(JavaClass.ForName("android.runtime.JavaProxyThrowable"));
            }));

        // Set options for the managed SDK that depend on the Android SDK. (The user will not be able to modify these.)
        options.AddEventProcessor(new AndroidEventProcessor(androidOptions!));
        options.CrashedLastRun = () => Java.Sentry.IsCrashedLastRun()?.BooleanValue() is true;
        options.EnableScopeSync = true;
        options.ScopeObserver = new AndroidScopeObserver(options);

        // TODO: Pause/Resume
    }

    private static void AndroidEnvironment_UnhandledExceptionRaiser(object? _, RaiseThrowableEventArgs e)
    {
        e.Exception.Data[Mechanism.HandledKey] = e.Handled;
        e.Exception.Data[Mechanism.MechanismKey] = "UnhandledExceptionRaiser";
        CaptureException(e.Exception);
        if (!e.Handled)
        {
            Close();
        }
    }

    private static string? GetDefaultReleaseString()
    {
        var packageName = AppContext.PackageName;
        if (packageName == null)
        {
            return null;
        }

        var packageInfo = AppContext.PackageManager?.GetPackageInfo(packageName, PackageInfoFlags.Permissions);
        return packageInfo == null ? null : $"{packageName}@{packageInfo.VersionName}+{packageInfo.GetVersionCode()}";
    }

    private static string? GetDefaultDistributionString() => GetAndroidPackageVersionCode()?.ToString();

    private static long? GetAndroidPackageVersionCode()
    {
        var packageName = AppContext.PackageName;
        if (packageName == null)
        {
            return null;
        }

        var packageInfo = AppContext.PackageManager?.GetPackageInfo(packageName, PackageInfoFlags.Permissions);
        return packageInfo?.GetVersionCode();
    }

    private static long? GetVersionCode(this PackageInfo packageInfo)
    {
        // The value comes from different property depending on Android version
        if (AndroidBuild.VERSION.SdkInt >= BuildVersionCodes.P)
        {
#pragma warning disable CA1416
            // callsite only reachable on Android >= P (28)
            return packageInfo.LongVersionCode;
#pragma warning restore CA1416
        }

#pragma warning disable CS0618
        // obsolete on Android >= P (28)
        return packageInfo.VersionCode;
#pragma warning restore CS0618
    }
}

using Sentry.Android;
using Sentry.Android.Callbacks;
using Sentry.Android.Extensions;
using Sentry.Extensibility;
using Sentry.Protocol;

// ReSharper disable once CheckNamespace
namespace Sentry;

public static partial class SentrySdk
{
    private static AndroidContext? AndroidContext;

    /// <summary>
    /// Initializes the SDK for Android, with an optional configuration options callback.
    /// </summary>
    /// <param name="context">The Android application context.</param>
    /// <param name="configureOptions">The configuration options callback.</param>
    /// <returns>An object that should be disposed when the application terminates.</returns>
    public static IDisposable Init(AndroidContext context, Action<SentryOptions>? configureOptions)
    {
        var options = new SentryOptions();
        configureOptions?.Invoke(options);
        return Init(context, options);
    }

    /// <summary>
    /// Initializes the SDK for Android, using a configuration options instance.
    /// </summary>
    /// <param name="context">The Android application context.</param>
    /// <param name="options">The configuration options instance.</param>
    /// <returns>An object that should be disposed when the application terminates.</returns>
    public static IDisposable Init(AndroidContext context, SentryOptions options)
    {
        AndroidContext = context;
        return Init(options);
    }

    private static void InitSentryAndroidSdk(SentryOptions options)
    {
        // Set options for the managed SDK that don't depend on the Android SDK
        options.AutoSessionTracking = true;
        options.IsGlobalModeEnabled = true;

        // "Best" mode throws permission exception on Android
        options.DetectStartupTime = StartupTimeDetectionMode.Fast;

        // Make sure we capture managed exceptions from the Android environment
        AndroidEnvironment.UnhandledExceptionRaiser += AndroidEnvironment_UnhandledExceptionRaiser;

        // Now initialize the Android SDK if we have been given an AndroidContext
        var context = AndroidContext;
        if (context == null)
        {
            options.LogWarning("Running on Android, but did not initialize Sentry with an AndroidContext. " +
                               "The embedded Sentry Android SDK is disabled. " +
                               "Call SentrySdk.Init(AndroidContext, SentryOptions) instead.");
            return;
        }

        SentryAndroidOptions? androidOptions = null;
        SentryAndroid.Init(context, new JavaLogger(options),
            new OptionsConfigurationCallback(o =>
            {
                // Capture the android options reference on the outer scope
                androidOptions = o;

                // TODO: Should we set the DistinctId to match the one used by GlobalSessionManager?
                //o.DistinctId = ?

                // TODO: Should we just copy EnableScopeSync, or should we always set it true for Android?
                //       And also, Do we need to pass a scope observer?
                //o.EnableScopeSync = options.EnableScopeSync; ??

                // These options are copied over from our SentryOptions
                o.AttachStacktrace = options.AttachStacktrace;
                o.Debug = options.Debug;
                o.DiagnosticLevel = options.DiagnosticLevel.ToJavaSentryLevel();
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

                var javaTags = o.Tags;
                foreach (var tag in options.DefaultTags)
                {
                    javaTags.Add(tag);
                }

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
                o.ProfilingTracesIntervalMillis = (int)options.Android.ProfilingTracesInterval.TotalMilliseconds;

                // These options are in Java.SentryOptions but not ours
                o.AttachThreads = options.Android.AttachThreads;
                o.ConnectionTimeoutMillis = (int)options.Android.ConnectionTimeout.TotalMilliseconds;
                o.Dist = options.Android.Distribution;
                o.EnableNdk = options.Android.EnableNdk;
                o.EnableShutdownHook = options.Android.EnableShutdownHook;
                o.EnableUncaughtExceptionHandler = options.Android.EnableUncaughtExceptionHandler;
                o.ProfilingEnabled = options.Android.ProfilingEnabled;
                o.PrintUncaughtStackTrace = options.Android.PrintUncaughtStackTrace;
                o.ReadTimeoutMillis = (int)options.Android.ReadTimeout.TotalMilliseconds;

                // In-App Excludes and Includes to be passed to the Android SDK
                options.Android.InAppExclude?.ToList().ForEach(x => o.AddInAppExclude(x));
                options.Android.InAppInclude?.ToList().ForEach(x => o.AddInAppInclude(x));

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

        // Set options for the managed SDK that depend on the Android SDK
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
}

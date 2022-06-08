using Sentry.Android;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry
{
    public static partial class SentrySdk
    {
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
            // TODO: Pause/Resume
            options.AutoSessionTracking = true;
            options.IsGlobalModeEnabled = true;
            options.AddEventProcessor(new DelegateEventProcessor(evt =>
            {
                if (AndroidBuild.SupportedAbis is { } abis)
                {
                    evt.Contexts.Device.Architecture = abis[0];
                }
                else
                {
#pragma warning disable CS0618 // Type or member is obsolete
                    evt.Contexts.Device.Architecture = AndroidBuild.CpuAbi;
#pragma warning restore CS0618 // Type or member is obsolete
                }

                evt.Contexts.Device.Manufacturer = AndroidBuild.Manufacturer;

                return evt;
            }));

            SentryAndroid.Init(context, new JavaLogger(options),
                new OptionsConfigurationCallback(o =>
                {
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
                    o.TracesSampleRate = (JavaDouble?)options.TracesSampleRate;

                    if (options.CacheDirectoryPath != null)
                    {
                        // Set a separate cache path for the native SDK so we don't step on the managed one
                        o.CacheDirPath = Path.Combine(options.CacheDirectoryPath, "native");
                    }

                    foreach (var tag in options.DefaultTags)
                    {
                        o.SetTag(tag.Key, tag.Value);
                    }

                    if (options.BeforeBreadcrumb != null)
                    {
                        o.BeforeBreadcrumb = new BeforeBreadcrumbCallback(options.BeforeBreadcrumb, options.DiagnosticLogger, o);
                    }

                    if (options.BeforeSend != null)
                    {
                        o.BeforeSend = new BeforeSendCallback(options.BeforeSend, options.DiagnosticLogger, o);
                    }

                    if (options.TracesSampler != null)
                    {
                        o.TracesSampler = new TracesSamplerCallback(options.TracesSampler);
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

            options.CrashedLastRun = () => Java.Sentry.IsCrashedLastRun()?.BooleanValue() is true;

            AndroidEnvironment.UnhandledExceptionRaiser += AndroidEnvironment_UnhandledExceptionRaiser;

            return Init(options);
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
}

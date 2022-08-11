using Sentry.iOS;
using Sentry.iOS.Extensions;

// ReSharper disable once CheckNamespace
namespace Sentry;

public static partial class SentrySdk
{
    private static void InitSentryCocoaSdk(SentryOptions options)
    {
        // Workaround for https://github.com/xamarin/xamarin-macios/issues/15252
        ObjCRuntime.Runtime.MarshalManagedException += (_, args) =>
        {
            args.ExceptionMode = ObjCRuntime.MarshalManagedExceptionMode.UnwindNativeCode;
        };

        // Set options for the managed SDK that don't depend on the Cocoa SDK
        options.AutoSessionTracking = true;
        options.IsGlobalModeEnabled = true;

        // "Best" mode throws platform not supported exception.  Use "Fast" mode instead.
        options.DetectStartupTime = StartupTimeDetectionMode.Fast;

        // Now initialize the Cocoa SDK
        SentryCocoa.SentryOptions? cocoaOptions = null;
        SentryCocoa.SentrySDK.StartWithConfigureOptions(o =>
        {
            // Capture the Cocoa options reference on the outer scope
            cocoaOptions = o;

            // TODO: Equivalent of Android options?
            // o.DistinctId
            // o.EnableScopeSync

            // These options are copied over from our SentryOptions
            o.AttachStacktrace = options.AttachStacktrace;
            o.Debug = options.Debug;
            o.DiagnosticLevel = options.DiagnosticLevel.ToCocoaSentryLevel();
            o.Dsn = options.Dsn;
            o.EnableAutoSessionTracking = options.AutoSessionTracking;
            o.Environment = options.Environment;
            //o.FlushTimeoutMillis = (long)options.InitCacheFlushTimeout.TotalMilliseconds;
            o.MaxAttachmentSize = (nuint) options.MaxAttachmentSize;
            o.MaxBreadcrumbs = (nuint) options.MaxBreadcrumbs;
            o.MaxCacheItems = (nuint) options.MaxCacheItems;
            // o.MaxQueueSize = options.MaxQueueItems;
            o.ReleaseName = options.Release;
            o.SampleRate = options.SampleRate;
            o.SendClientReports = options.SendClientReports;
            o.SendDefaultPii = options.SendDefaultPii;
            o.SessionTrackingIntervalMillis = (nuint) options.AutoSessionTrackingInterval.TotalMilliseconds;
            // o.ShutdownTimeoutMillis = (long)options.ShutdownTimeout.TotalMilliseconds;

            // NOTE: options.CacheDirectoryPath - No option for this in Sentry Cocoa, but caching is still enabled
            // https://github.com/getsentry/sentry-cocoa/issues/1051

            // NOTE: Tags in options.DefaultTags should not be passed down, because we already call SetTag on each
            //       one when sending events, which is relayed through the scope observer.

            if (options.BeforeBreadcrumb is { } beforeBreadcrumb)
            {
                // Note: Nullable return is allowed but delegate is generated incorrectly
                o.BeforeBreadcrumb = b => beforeBreadcrumb(b.ToBreadcrumb(options.DiagnosticLogger))?
                    .ToCocoaBreadcrumb()!;
            }

            // These options we have behind feature flags
            if (options.iOS.EnableCocoaSdkTracing)
            {
                o.TracesSampleRate = options.TracesSampleRate;

                if (options.TracesSampler is { } tracesSampler)
                {
                    // Note: Nullable return is allowed but delegate is generated incorrectly
                    o.TracesSampler = context => tracesSampler(context.ToTransactionSamplingContext())!;
                }
            }

            // TODO: Work on below (copied from Android implementation - needs updating)

            // if (options.iOS.EnableCocoaSdkBeforeSend && options.BeforeSend is { } beforeSend)
            // {
            //     // Note: Nullable return is allowed but delegate is generated incorrectly
            //     o.BeforeSend = evt => beforeSend(evt.ToSentryEvent(o))?.ToCocoaSentryEvent(options, o)!;
            // }

            //
            //         // These options we have behind feature flags
            //         if (options.Android.EnableAndroidSdkTracing)
            //         {
            //             o.TracesSampleRate = (JavaDouble?)options.TracesSampleRate;
            //
            //             if (options.TracesSampler is { } tracesSampler)
            //             {
            //                 o.TracesSampler = new TracesSamplerCallback(tracesSampler);
            //             }
            //         }
            //
            //         if (options.Android.EnableAndroidSdkBeforeSend && options.BeforeSend is { } beforeSend)
            //         {
            //             o.BeforeSend = new BeforeSendCallback(beforeSend, options, o);
            //         }
            //
            //         // These options are from SentrycocoaOptions
            //         o.AttachScreenshot = options.Android.AttachScreenshot;
            //         o.AnrEnabled = options.Android.AnrEnabled;
            //         o.AnrReportInDebug = options.Android.AnrReportInDebug;
            //         o.AnrTimeoutIntervalMillis = (long)options.Android.AnrTimeoutInterval.TotalMilliseconds;
            //         o.EnableActivityLifecycleBreadcrumbs = options.Android.EnableActivityLifecycleBreadcrumbs;
            //         o.EnableAutoActivityLifecycleTracing = options.Android.EnableAutoActivityLifecycleTracing;
            //         o.EnableActivityLifecycleTracingAutoFinish = options.Android.EnableActivityLifecycleTracingAutoFinish;
            //         o.EnableAppComponentBreadcrumbs = options.Android.EnableAppComponentBreadcrumbs;
            //         o.EnableAppLifecycleBreadcrumbs = options.Android.EnableAppLifecycleBreadcrumbs;
            //         o.EnableSystemEventBreadcrumbs = options.Android.EnableSystemEventBreadcrumbs;
            //         o.EnableUserInteractionBreadcrumbs = options.Android.EnableUserInteractionBreadcrumbs;
            //         o.EnableUserInteractionTracing = options.Android.EnableUserInteractionTracing;
            //         o.ProfilingTracesIntervalMillis = (int)options.Android.ProfilingTracesInterval.TotalMilliseconds;
            //
            //         // These options are in Java.SentryOptions but not ours
            //         o.AttachThreads = options.Android.AttachThreads;
            //         o.ConnectionTimeoutMillis = (int)options.Android.ConnectionTimeout.TotalMilliseconds;
            //         o.Dist = options.Android.Distribution;
            //         o.EnableNdk = options.Android.EnableNdk;
            //         o.EnableShutdownHook = options.Android.EnableShutdownHook;
            //         o.EnableUncaughtExceptionHandler = options.Android.EnableUncaughtExceptionHandler;
            //         o.ProfilingEnabled = options.Android.ProfilingEnabled;
            //         o.PrintUncaughtStackTrace = options.Android.PrintUncaughtStackTrace;
            //         o.ReadTimeoutMillis = (int)options.Android.ReadTimeout.TotalMilliseconds;
            //
            //         // In-App Excludes and Includes to be passed to the Android SDK
            //         options.Android.InAppExclude?.ToList().ForEach(x => o.AddInAppExclude(x));
            //         options.Android.InAppInclude?.ToList().ForEach(x => o.AddInAppInclude(x));
            //
            //         // These options are intentionally set and not exposed for modification
            //         o.EnableExternalConfiguration = false;
            //         o.EnableDeduplication = false;
            //         o.AttachServerName = false;
            //
            //         // These options are intentionally not expose or modified
            //         //o.MaxRequestBodySize   // N/A for Android apps
            //         //o.MaxSpans             // See https://github.com/getsentry/sentry-dotnet/discussions/1698
            //
            //         // Don't capture managed exceptions in the native SDK, since we already capture them in the managed SDK
            //         o.AddIgnoredExceptionForType(JavaClass.ForName("android.runtime.JavaProxyThrowable"));

        });

        // Set options for the managed SDK that depend on the Cocoa SDK
        options.AddEventProcessor(new IosEventProcessor(cocoaOptions!));
        options.CrashedLastRun = () => SentryCocoa.SentrySDK.CrashedLastRun;
        options.EnableScopeSync = true;
        options.ScopeObserver = new IosScopeObserver(options);

        // TODO: Pause/Resume

    }
}

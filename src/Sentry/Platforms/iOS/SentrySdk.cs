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

            // These options are copied over from our SentryOptions
            o.AttachStacktrace = options.AttachStacktrace;
            o.Debug = options.Debug;
            o.DiagnosticLevel = options.DiagnosticLevel.ToCocoaSentryLevel();
            o.Dsn = options.Dsn;
            o.EnableAutoSessionTracking = options.AutoSessionTracking;
            o.Environment = options.Environment;
            o.MaxAttachmentSize = (nuint) options.MaxAttachmentSize;
            o.MaxBreadcrumbs = (nuint) options.MaxBreadcrumbs;
            o.MaxCacheItems = (nuint) options.MaxCacheItems;
            o.ReleaseName = options.Release;
            o.SampleRate = options.SampleRate;
            o.SendClientReports = options.SendClientReports;
            o.SendDefaultPii = options.SendDefaultPii;
            o.SessionTrackingIntervalMillis = (nuint) options.AutoSessionTrackingInterval.TotalMilliseconds;

            // These options are not available in the Sentry Cocoa SDK
            // o.? = options.InitCacheFlushTimeout;
            // o.? = options.MaxQueueItems;
            // o.? = options.ShutdownTimeout;

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

            // TODO: Finish SentryEventExtensions to enable this
            // if (options.iOS.EnableCocoaSdkBeforeSend && options.BeforeSend is { } beforeSend)
            // {
            //     // Note: Nullable return is allowed but delegate is generated incorrectly
            //     o.BeforeSend = evt => beforeSend(evt.ToSentryEvent(o))?.ToCocoaSentryEvent(options, o)!;
            // }

            // These options are from Cocoa's SentryOptions
            o.AttachScreenshot = options.iOS.AttachScreenshot;
            o.AppHangTimeoutInterval = options.iOS.AppHangTimeoutInterval.TotalSeconds;
            o.IdleTimeout = options.iOS.IdleTimeout.TotalSeconds;
            o.Dist = options.iOS.Distribution;
            o.EnableAppHangTracking = options.iOS.EnableAppHangTracking;
            o.EnableAutoBreadcrumbTracking = options.iOS.EnableAutoBreadcrumbTracking;
            o.EnableAutoPerformanceTracking = options.iOS.EnableAutoPerformanceTracking;
            o.EnableAutoSessionTracking = options.iOS.EnableAutoSessionTracking;
            o.EnableCoreDataTracking = options.iOS.EnableCoreDataTracking;
            o.EnableFileIOTracking = options.iOS.EnableFileIOTracking;
            o.EnableNetworkBreadcrumbs = options.iOS.EnableNetworkBreadcrumbs;
            o.EnableNetworkTracking = options.iOS.EnableNetworkTracking;
            o.EnableOutOfMemoryTracking = options.iOS.EnableOutOfMemoryTracking;
            o.EnableProfiling = options.iOS.EnableProfiling;
            o.EnableSwizzling = options.iOS.EnableSwizzling;
            o.EnableUIViewControllerTracking = options.iOS.EnableUIViewControllerTracking;
            o.EnableUserInteractionTracing = options.iOS.EnableUserInteractionTracing;
            o.StitchAsyncCode = options.iOS.StitchAsyncCode;

            // In-App Excludes and Includes to be passed to the Cocoa SDK
            options.iOS.InAppExcludes?.ForEach(x => o.AddInAppExclude(x));
            options.iOS.InAppIncludes?.ForEach(x => o.AddInAppInclude(x));

            // These options are intentionally not expose or modified
            // o.Enabled;
            // o.SdkInfo

            // // Don't capture managed exceptions in the native SDK, since we already capture them in the managed SDK
            // o.AddIgnoredExceptionForType(JavaClass.ForName("android.runtime.JavaProxyThrowable"));

        });

        // Set options for the managed SDK that depend on the Cocoa SDK
        options.AddEventProcessor(new IosEventProcessor(cocoaOptions!));
        options.CrashedLastRun = () => SentryCocoa.SentrySDK.CrashedLastRun;
        options.EnableScopeSync = true;
        options.ScopeObserver = new IosScopeObserver(options);

        // TODO: Pause/Resume

    }
}

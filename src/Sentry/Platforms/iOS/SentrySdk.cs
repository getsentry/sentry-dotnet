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

            // TODO: Finish SentryEventExtensions to enable these
            //
            // if (options.iOS.EnableCocoaSdkBeforeSend && options.BeforeSend is { } beforeSend)
            // {
            //     // Note: Nullable return is allowed but delegate is generated incorrectly
            //     o.BeforeSend = evt => beforeSend(evt.ToSentryEvent(o))?.ToCocoaSentryEvent(options, o)!;
            // }
            //
            // if (options.iOS.OnCrashedLastRun is { } onCrashedLastRun)
            // {
            //     o.OnCrashedLastRun = evt => onCrashedLastRun(evt.ToSentryEvent(o));
            // }

            // These options are from Cocoa's SentryOptions
            o.AttachScreenshot = options.iOS.AttachScreenshot;
            o.AppHangTimeoutInterval = options.iOS.AppHangTimeoutInterval.TotalSeconds;
            o.IdleTimeout = options.iOS.IdleTimeout.TotalSeconds;
            o.Dist = options.iOS.Distribution;
            o.EnableAppHangTracking = options.iOS.EnableAppHangTracking;
            o.EnableAutoBreadcrumbTracking = options.iOS.EnableAutoBreadcrumbTracking;
            o.EnableAutoPerformanceTracking = options.iOS.EnableAutoPerformanceTracking;
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
            o.UrlSessionDelegate = options.iOS.UrlSessionDelegate;

            // In-App Excludes and Includes to be passed to the Cocoa SDK
            options.iOS.InAppExcludes?.ForEach(x => o.AddInAppExclude(x));
            options.iOS.InAppIncludes?.ForEach(x => o.AddInAppInclude(x));

            // These options are intentionally not expose or modified
            // o.Enabled
            // o.SdkInfo
            // o.Integrations
            // o.DefaultIntegrations

            // When we have an unhandled managed exception, we send that to Sentry twice - once managed and once native.
            // The managed exception is what a .NET developer would expect, and it is sent by the Sentry.NET SDK
            // But we also get a native SIGABRT since it crashed the application, which is sent by the Sentry Cocoa SDK.
            // This is partially due to our setting ObjCRuntime.MarshalManagedExceptionMode.UnwindNativeCode above.
            // Thankfully, we can see Xamarin's unhandled exception handler on the stack trace, so we can filter them out.
            // Here is the function that calls abort(), which we will use as a filter:
            // https://github.com/xamarin/xamarin-macios/blob/c55fbdfef95028ba03d0f7a35aebca03bd76f852/runtime/runtime.m#L1114-L1122
            o.BeforeSend = evt =>
            {
                // There should only be one exception on the event in this case
                if (evt.Exceptions?.Length == 1)
                {
                    // It will match the following characteristics
                    var ex = evt.Exceptions[0];
                    if (ex.Type == "SIGABRT" && ex.Value == "Signal 6, Code 0" &&
                        ex.Stacktrace?.Frames.Any(f => f.Function == "xamarin_unhandled_exception_handler") is true)
                    {
                        // Don't sent it
                        return null!;
                    }
                }

                // Other event, send as normal
                return evt;
            };

        });

        // Set options for the managed SDK that depend on the Cocoa SDK
        options.AddEventProcessor(new IosEventProcessor(cocoaOptions!));
        options.CrashedLastRun = () => SentryCocoa.SentrySDK.CrashedLastRun;
        options.EnableScopeSync = true;
        options.ScopeObserver = new IosScopeObserver(options);

        // TODO: Pause/Resume
    }
}

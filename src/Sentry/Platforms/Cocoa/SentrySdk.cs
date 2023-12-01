using Sentry.Cocoa;
using Sentry.Cocoa.Extensions;

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

        // Set default release and distribution
        options.Release ??= GetDefaultReleaseString();
        options.Distribution ??= GetDefaultDistributionString();

        // Set options for the Cocoa SDK
        var cocoaOptions = new SentryCocoaOptions();

        // These options are copied over from our SentryOptions
        cocoaOptions.AttachStacktrace = options.AttachStacktrace;
        cocoaOptions.Debug = options.Debug;
        cocoaOptions.DiagnosticLevel = options.DiagnosticLevel.ToCocoaSentryLevel();
        cocoaOptions.Dsn = options.Dsn;
        cocoaOptions.EnableAutoSessionTracking = options.AutoSessionTracking;
        cocoaOptions.EnableCaptureFailedRequests = options.CaptureFailedRequests;
        cocoaOptions.FailedRequestStatusCodes = GetFailedRequestStatusCodes(options.FailedRequestStatusCodes);
        cocoaOptions.MaxAttachmentSize = (nuint) options.MaxAttachmentSize;
        cocoaOptions.MaxBreadcrumbs = (nuint) options.MaxBreadcrumbs;
        cocoaOptions.MaxCacheItems = (nuint) options.MaxCacheItems;
        cocoaOptions.ReleaseName = options.Release;
        cocoaOptions.SampleRate = options.SampleRate;
        cocoaOptions.SendClientReports = options.SendClientReports;
        cocoaOptions.SendDefaultPii = options.SendDefaultPii;
        cocoaOptions.SessionTrackingIntervalMillis = (nuint) options.AutoSessionTrackingInterval.TotalMilliseconds;

        if (options.Environment is { } environment)
        {
            cocoaOptions.Environment = environment;
        }

        // These options are not available in the Sentry Cocoa SDK
        // cocoaOptions.? = options.InitCacheFlushTimeout;
        // cocoaOptions.? = options.MaxQueueItems;
        // cocoaOptions.? = options.ShutdownTimeout;

        // NOTE: options.CacheDirectoryPath - No option for this in Sentry Cocoa, but caching is still enabled
        // https://github.com/getsentry/sentry-cocoa/issues/1051

        // NOTE: Tags in options.DefaultTags should not be passed down, because we already call SetTag on each
        //       one when sending events, which is relayed through the scope observer.

        if (options.BeforeBreadcrumbInternal is { } beforeBreadcrumb)
        {
            cocoaOptions.BeforeBreadcrumb = b =>
            {
                // Note: The Cocoa SDK doesn't yet support hints.
                // See https://github.com/getsentry/sentry-cocoa/issues/2325
                var hint = new Hint();
                var breadcrumb = b.ToBreadcrumb(options.DiagnosticLogger);
                var result = beforeBreadcrumb(breadcrumb, hint)?.ToCocoaBreadcrumb();

                // Note: Nullable result is allowed but delegate is generated incorrectly
                // See https://github.com/xamarin/xamarin-macios/issues/15299#issuecomment-1201863294
                return result!;
            };
        }

        // These options we have behind feature flags
        if (options is {IsPerformanceMonitoringEnabled: true, Cocoa.EnableCocoaSdkTracing: true})
        {
            if (options.EnableTracing != null)
            {
                cocoaOptions.EnableTracing = options.EnableTracing.Value;
            }

            cocoaOptions.TracesSampleRate = options.TracesSampleRate;

            if (options.TracesSampler is { } tracesSampler)
            {
                cocoaOptions.TracesSampler = cocoaContext =>
                {
                    var context = cocoaContext.ToTransactionSamplingContext();
                    var result = tracesSampler(context);

                    // Note: Nullable result is allowed but delegate is generated incorrectly
                    // See https://github.com/xamarin/xamarin-macios/issues/15299#issuecomment-1201863294
                    return result!;
                };
            }
        }

        // TODO: Finish SentryEventExtensions to enable these

        // if (options.Cocoa.EnableCocoaSdkBeforeSend && options.BeforeSend is { } beforeSend)
        // {
        //     cocoaOptions.BeforeSend = evt =>
        //     {
        //         var sentryEvent = evt.ToSentryEvent(cocoaOptions);
        //         var result = beforeSend(sentryEvent)?.ToCocoaSentryEvent(options, cocoaOptions);
        //
        //         // Note: Nullable result is allowed but delegate is generated incorrectly
        //         // See https://github.com/xamarin/xamarin-macios/issues/15299#issuecomment-1201863294
        //         return result!;
        //     };
        // }

        // if (options.Cocoa.OnCrashedLastRun is { } onCrashedLastRun)
        // {
        //     cocoaOptions.OnCrashedLastRun = evt =>
        //     {
        //         var sentryEvent = evt.ToSentryEvent(cocoaOptions);
        //         onCrashedLastRun(sentryEvent);
        //     };
        // }

        // These options are from Cocoa's SentryOptions
        cocoaOptions.AttachScreenshot = options.Cocoa.AttachScreenshot;
        cocoaOptions.AppHangTimeoutInterval = options.Cocoa.AppHangTimeoutInterval.TotalSeconds;
        cocoaOptions.IdleTimeout = options.Cocoa.IdleTimeout.TotalSeconds;
        cocoaOptions.Dist = options.Distribution;
        cocoaOptions.EnableAppHangTracking = options.Cocoa.EnableAppHangTracking;
        cocoaOptions.EnableAutoBreadcrumbTracking = options.Cocoa.EnableAutoBreadcrumbTracking;
        cocoaOptions.EnableAutoPerformanceTracing = options.Cocoa.EnableAutoPerformanceTracing;
        cocoaOptions.EnableCoreDataTracing = options.Cocoa.EnableCoreDataTracing;
        cocoaOptions.EnableFileIOTracing = options.Cocoa.EnableFileIOTracing;
        cocoaOptions.EnableNetworkBreadcrumbs = options.Cocoa.EnableNetworkBreadcrumbs;
        cocoaOptions.EnableNetworkTracking = options.Cocoa.EnableNetworkTracking;
        cocoaOptions.EnableWatchdogTerminationTracking = options.Cocoa.EnableWatchdogTerminationTracking;
        cocoaOptions.EnableSwizzling = options.Cocoa.EnableSwizzling;
        cocoaOptions.EnableUIViewControllerTracing = options.Cocoa.EnableUIViewControllerTracing;
        cocoaOptions.EnableUserInteractionTracing = options.Cocoa.EnableUserInteractionTracing;
        cocoaOptions.UrlSessionDelegate = options.Cocoa.UrlSessionDelegate;

        // StitchAsyncCode removed from Cocoa SDK in 8.6.0 with https://github.com/getsentry/sentry-cocoa/pull/2973
        // cocoaOptions.StitchAsyncCode = options.Cocoa.StitchAsyncCode;

        // In-App Excludes and Includes to be passed to the Cocoa SDK
        options.Cocoa.InAppExcludes?.ForEach(x => cocoaOptions.AddInAppExclude(x));
        options.Cocoa.InAppIncludes?.ForEach(x => cocoaOptions.AddInAppInclude(x));

        // These options are intentionally not expose or modified
        // cocoaOptions.Enabled
        // cocoaOptions.SdkInfo
        // cocoaOptions.Integrations
        // cocoaOptions.DefaultIntegrations
        // cocoaOptions.EnableProfiling  (deprecated)

        // When we have an unhandled managed exception, we send that to Sentry twice - once managed and once native.
        // The managed exception is what a .NET developer would expect, and it is sent by the Sentry.NET SDK
        // But we also get a native SIGABRT since it crashed the application, which is sent by the Sentry Cocoa SDK.
        // This is partially due to our setting ObjCRuntime.MarshalManagedExceptionMode.UnwindNativeCode above.
        // Thankfully, we can see Xamarin's unhandled exception handler on the stack trace, so we can filter them out.
        // Here is the function that calls abort(), which we will use as a filter:
        // https://github.com/xamarin/xamarin-macios/blob/c55fbdfef95028ba03d0f7a35aebca03bd76f852/runtime/runtime.m#L1114-L1122
        cocoaOptions.BeforeSend = evt =>
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

        // Set hybrid SDK name
        SentryCocoaHybridSdk.SetSdkName("sentry.cocoa.dotnet");

        // Now initialize the Cocoa SDK
        SentryCocoaSdk.StartWithOptions(cocoaOptions);

        // Set options for the managed SDK that depend on the Cocoa SDK. (The user will not be able to modify these.)
        options.AddEventProcessor(new CocoaEventProcessor(cocoaOptions));
        options.CrashedLastRun = () => SentryCocoaSdk.CrashedLastRun;
        options.EnableScopeSync = true;
        options.ScopeObserver = new CocoaScopeObserver(options);

        // TODO: Pause/Resume
    }

    private static string GetDefaultReleaseString()
    {
        var packageName = GetBundleValue("CFBundleIdentifier");
        var packageVersion = GetBundleValue("CFBundleShortVersionString");
        var buildVersion = GetBundleValue("CFBundleVersion");

        return $"{packageName}@{packageVersion}+{buildVersion}";
    }

    private static string GetDefaultDistributionString() => GetBundleValue("CFBundleVersion");

    private static string GetBundleValue(string key) => NSBundle.MainBundle.ObjectForInfoDictionary(key).ToString();

    private static CocoaSdk.SentryHttpStatusCodeRange[] GetFailedRequestStatusCodes(IList<HttpStatusCodeRange> httpStatusCodeRanges)
    {
        var nativeRanges = new CocoaSdk.SentryHttpStatusCodeRange[httpStatusCodeRanges.Count];
        for (var i = 0; i < httpStatusCodeRanges.Count; i++)
        {
            var range = httpStatusCodeRanges[i];
            nativeRanges[i] = new CocoaSdk.SentryHttpStatusCodeRange(range.Start, range.End);
        }

        return nativeRanges;
    }
}

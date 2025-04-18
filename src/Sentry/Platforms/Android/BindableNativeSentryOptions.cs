// ReSharper disable once CheckNamespace
namespace Sentry;

internal partial class BindableSentryOptions
{
    public NativeOptions Native { get; } = new NativeOptions();

    /// <summary>
    /// Provides additional options for the Android platform.
    /// </summary>
    public class NativeOptions
    {
        public bool? AnrEnabled { get; set; }
        public bool? AnrReportInDebug { get; set; }
        public TimeSpan? AnrTimeoutInterval { get; set; }
        public bool? AttachScreenshot { get; set; }
        public bool? EnableActivityLifecycleBreadcrumbs { get; set; }
        public bool? EnableAppComponentBreadcrumbs { get; set; }
        public bool? EnableAppLifecycleBreadcrumbs { get; set; }
        public bool? EnableRootCheck { get; set; }
        public bool? EnableNetworkEventBreadcrumbs { get; set; }
        public bool? EnableSystemEventBreadcrumbs { get; set; }
        public bool? EnableUserInteractionBreadcrumbs { get; set; }
        public bool? EnableAutoActivityLifecycleTracing { get; set; }
        public bool? EnableActivityLifecycleTracingAutoFinish { get; set; }
        public bool? EnableUserInteractionTracing { get; set; }
        public bool? AttachThreads { get; set; }
        public TimeSpan? ConnectionTimeout { get; set; }
        public bool? EnableNdk { get; set; }
        public bool? EnableShutdownHook { get; set; }
        public bool? EnableUncaughtExceptionHandler { get; set; }
        public bool? PrintUncaughtStackTrace { get; set; }
        public double? ProfilesSampleRate { get; set; }
        public TimeSpan? ReadTimeout { get; set; }
        public bool? EnableTracing { get; set; }
        public bool? EnableBeforeSend { get; set; }

        public NativeExperimentalOptions ExperimentalOptions { get; set; } = new();

        internal class NativeExperimentalOptions
        {
            public NativeSentryReplayOptions SessionReplay { get; set; } = new();
        }

        internal class NativeSentryReplayOptions
        {
            public double? OnErrorSampleRate { get; set; }
            public double? SessionSampleRate { get; set; }
            public bool RedactAllImages { get; set; }
            public bool RedactAllText { get; set; }
        }

        public void ApplyTo(SentryOptions.NativeOptions options)
        {
            options.AnrEnabled = AnrEnabled ?? options.AnrEnabled;
            options.AnrReportInDebug = AnrReportInDebug ?? options.AnrReportInDebug;
            options.AnrTimeoutInterval = AnrTimeoutInterval ?? options.AnrTimeoutInterval;
            options.AttachScreenshot = AttachScreenshot ?? options.AttachScreenshot;
            options.EnableActivityLifecycleBreadcrumbs = EnableActivityLifecycleBreadcrumbs ?? options.EnableActivityLifecycleBreadcrumbs;
            options.EnableAppComponentBreadcrumbs = EnableAppComponentBreadcrumbs ?? options.EnableAppComponentBreadcrumbs;
            options.EnableAppLifecycleBreadcrumbs = EnableAppLifecycleBreadcrumbs ?? options.EnableAppLifecycleBreadcrumbs;
            options.EnableRootCheck = EnableRootCheck ?? options.EnableRootCheck;
            options.EnableNetworkEventBreadcrumbs = EnableNetworkEventBreadcrumbs ?? options.EnableNetworkEventBreadcrumbs;
            options.EnableSystemEventBreadcrumbs = EnableSystemEventBreadcrumbs ?? options.EnableSystemEventBreadcrumbs;
            options.EnableUserInteractionBreadcrumbs = EnableUserInteractionBreadcrumbs ?? options.EnableUserInteractionBreadcrumbs;
            options.EnableAutoActivityLifecycleTracing = EnableAutoActivityLifecycleTracing ?? options.EnableAutoActivityLifecycleTracing;
            options.EnableActivityLifecycleTracingAutoFinish = EnableActivityLifecycleTracingAutoFinish ?? options.EnableActivityLifecycleTracingAutoFinish;
            options.EnableUserInteractionTracing = EnableUserInteractionTracing ?? options.EnableUserInteractionTracing;
            options.AttachThreads = AttachThreads ?? options.AttachThreads;
            options.ConnectionTimeout = ConnectionTimeout ?? options.ConnectionTimeout;
            options.EnableNdk = EnableNdk ?? options.EnableNdk;
            options.EnableShutdownHook = EnableShutdownHook ?? options.EnableShutdownHook;
            options.EnableUncaughtExceptionHandler = EnableUncaughtExceptionHandler ?? options.EnableUncaughtExceptionHandler;
            options.PrintUncaughtStackTrace = PrintUncaughtStackTrace ?? options.PrintUncaughtStackTrace;
            options.ProfilesSampleRate = ProfilesSampleRate ?? options.ProfilesSampleRate;
            options.ReadTimeout = ReadTimeout ?? options.ReadTimeout;
            options.EnableTracing = EnableTracing ?? options.EnableTracing;
            options.EnableBeforeSend = EnableBeforeSend ?? options.EnableBeforeSend;

            if (ExperimentalOptions.SessionReplay.OnErrorSampleRate is { } errorSampleRate)
            {
#pragma warning disable CA1422
                options.ExperimentalOptions.SessionReplay.OnErrorSampleRate = errorSampleRate;
#pragma warning restore CA1422
            }
            if (ExperimentalOptions.SessionReplay.SessionSampleRate is { } sessionSampleRate)
            {
#pragma warning disable CA1422
                options.ExperimentalOptions.SessionReplay.SessionSampleRate = sessionSampleRate;
#pragma warning restore CA1422
            }
            ExperimentalOptions.SessionReplay.RedactAllText = options.ExperimentalOptions.SessionReplay.MaskAllText;
            ExperimentalOptions.SessionReplay.RedactAllImages = options.ExperimentalOptions.SessionReplay.MaskAllImages;
        }
    }
}

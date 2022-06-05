using System;
using System.Linq;

namespace Sentry
{
    public partial class SentryOptions
    {
        /// <summary>
        /// Exposes additional options for the Android platform.
        /// </summary>
        public AndroidOptions Android { get; } = new();

        /// <summary>
        /// Provides additional options for the Android platform.
        /// </summary>
        public class AndroidOptions
        {
            internal AndroidOptions() { }

            // From SentryAndroidOptions.java
            public bool AnrEnabled { get; set; } = true;
            public bool AnrReportInDebug { get; set; }
            public TimeSpan AnrTimeoutInterval { get; set; } = TimeSpan.FromSeconds(5);
            public bool AttachScreenshot { get; set; }
            public bool EnableActivityLifecycleBreadcrumbs { get; set; } = true;
            public bool EnableActivityLifecycleTracingAutoFinish { get; set; } = true;
            public bool EnableAppComponentBreadcrumbs { get; set; } = true;
            public bool EnableAppLifecycleBreadcrumbs { get; set; } = true;
            public bool EnableAutoActivityLifecycleTracing { get; set; } = true;
            public bool EnableSystemEventBreadcrumbs { get; set; } = true;
            public bool EnableUserInteractionBreadcrumbs { get; set; } = true;
            public bool EnableUserInteractionTracing { get; set; }
            public TimeSpan ProfilingTracesInterval { get; set; } = TimeSpan.FromMilliseconds(10);

            // From SentryOptions.Java
            public bool AttachServerName { get; set; } = true;
            public bool AttachThreads { get; set; }
            public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);
            public string? Distribution { get; set; }
            public bool EnableNdk { get; set; } = true;
            public bool EnableShutdownHoook { get; set; } = true;
            public bool EnableUncaughtExceptionHandler { get; set; } = true;
            public int MaxSpans { get; set; } = 1000;
            public bool PrintUncaughtStackTrace { get; set; }
            public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(5);

            internal string[]? InAppExclude { get; set; }
            internal string[]? InAppInclude { get; set; }

            public void AddInAppExclude(string prefix) =>
                InAppExclude = InAppExclude != null
                    ? InAppExclude.Concat(new[] { prefix }).ToArray()
                    : new[] { prefix };

            public void AddInAppInclude(string prefix) =>
                InAppInclude = InAppInclude != null
                    ? InAppInclude.Concat(new[] { prefix }).ToArray()
                    : new[] { prefix };
        }
    }
}

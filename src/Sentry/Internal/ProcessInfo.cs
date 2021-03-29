using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Sentry.Extensibility;

namespace Sentry.Internal
{
    internal class ProcessInfo
    {
        internal static ProcessInfo? Instance;

        /// <summary>
        /// When the code was initialized.
        /// </summary>
        internal DateTimeOffset StartupTime { get; set; }

        /// <summary>
        /// When the device was initialized.
        /// </summary>
        internal DateTimeOffset BootTime { get; }

        private SentryOptions _options { get; }

        internal ProcessInfo(SentryOptions options)
        {
            _options = options;
            StartupTime = DateTimeOffset.UtcNow;
            BootTime = DateTimeOffset.UtcNow - TimeSpan.FromTicks(Stopwatch.GetTimestamp());
        }

        /// <summary>
        /// By default, the StartupTime is set once SentryOptions is created.
        /// That way is computationally cheap but not so precise.
        /// You call this extension to give a better precision to the StartupTime at a cost of a small overhead to current
        /// Thread.
        /// </summary>
        internal void StartAccurateStartupTime()
        {
            _ = Task.Run(() =>
            {
                try
                {
                    using var proc = Process.GetCurrentProcess();
                    StartupTime = proc.StartTime.ToUniversalTime();
                }
                catch (Exception e)
                {
                    _options.DiagnosticLogger?.LogError("Failure to GetCurrentProcess", e);
                    //Ignore any exception and stay with the fallback DateTime.UtcNow value.
                }
            }).ConfigureAwait(false);
        }

    }
}

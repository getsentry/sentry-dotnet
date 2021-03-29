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
        /// StartupTime is set to UtcNow in this constructor.
        /// That's computationally cheap but not very precise.
        /// This method will give a better precision to the StartupTime at a cost
        /// of calling Process.GetCurrentProcess, on a thread pool thread.
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

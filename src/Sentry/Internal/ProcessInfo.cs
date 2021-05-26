using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.PlatformAbstractions;

namespace Sentry.Internal
{
    internal class ProcessInfo
    {
        internal static ProcessInfo? Instance;

        /// <summary>
        /// When the code was initialized.
        /// </summary>
        internal DateTimeOffset? StartupTime { get; private set; }

        /// <summary>
        /// When the device was initialized.
        /// </summary>
        internal DateTimeOffset? BootTime { get; }

        private readonly SentryOptions _options;
        private readonly Func<DateTimeOffset> _findPreciseStartupTime;
        private volatile Task _preciseAppStartupTask = Task.CompletedTask;

        // For testability
        internal Task PreciseAppStartupTask
        {
            get => _preciseAppStartupTask;
            private set => _preciseAppStartupTask = value;
        }

        internal ProcessInfo(
            SentryOptions options,
            Func<DateTimeOffset>? findPreciseStartupTime = null)
        {
            _options = options;
            _findPreciseStartupTime = findPreciseStartupTime ?? GetStartupTime;
            if (options.DetectStartupTime == StartupTimeDetectionMode.None)
            {
                _options.DiagnosticLogger?.LogDebug("Not detecting startup time due to option: {0}",
                    _options.DetectStartupTime);
                return;
            }

            // Fast
            var now = DateTimeOffset.UtcNow;
            StartupTime = now;
            try
            {
                BootTime = now.AddTicks(-Stopwatch.GetTimestamp()
                                        / (Stopwatch.Frequency
                                           / TimeSpan.TicksPerSecond));
            }
            catch (DivideByZeroException e)
            {
                // Seems to have failed on a single Windows Server 2012 on .NET Framework 4.8
                // https://github.com/getsentry/sentry-dotnet/issues/954
                _options.DiagnosticLogger?.LogError(
                    "Failed to find BootTime: GetTimestamp {0}, Frequency {1}, TicksPerSecond: {2}",
                    e, Stopwatch.GetTimestamp(), Stopwatch.Frequency, TimeSpan.TicksPerSecond);
            }

            // An opt-out to the more precise approach (mainly due to IL2CPP):
            // https://issuetracker.unity3d.com/issues/il2cpp-player-crashes-when-calling-process-dot-getcurrentprocess-dot-starttime
            if (_options.DetectStartupTime == StartupTimeDetectionMode.Best)
            {
                // StartupTime is set to UtcNow in this constructor.
                // That's computationally cheap but not very precise.
                // This method will give a better precision to the StartupTime at a cost
                // of calling Process.GetCurrentProcess, on a thread pool thread.
                PreciseAppStartupTask = Task.Run(() =>
                {
                    try
                    {
                        StartupTime = _findPreciseStartupTime();
                    }
                    catch (Exception e)
                    {
                        _options.DiagnosticLogger?.LogError("Failure getting precise App startup time.", e);
                        //Ignore any exception and stay with the less-precise DateTime.UtcNow value.
                    }
                }).ContinueWith(_ =>
                    // Let the actual task get collected
                    PreciseAppStartupTask = Task.CompletedTask);
            }
        }

        private static DateTimeOffset GetStartupTime()
        {
            using var proc = Process.GetCurrentProcess();
            return proc.StartTime.ToUniversalTime();
        }
    }
}

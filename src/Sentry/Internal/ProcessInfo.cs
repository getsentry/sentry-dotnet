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
                _options.LogDebug("Not detecting startup time due to option: {0}",
                    _options.DetectStartupTime);
                return;
            }

            // Fast
            var now = DateTimeOffset.UtcNow;
            StartupTime = now;
            long? timestamp = 0;
            try
            {
                timestamp = Stopwatch.GetTimestamp();
                BootTime = now.AddTicks(-timestamp.Value
                                        / (Stopwatch.Frequency
                                           / TimeSpan.TicksPerSecond));
            }
            // We can live with only `StartupTime` so lets handle the lack of `BootTime` and construct this object.
            catch (Exception e)
            {
                // DivideByZeroException: Seems to have failed on a single Windows Server 2012 on .NET Framework 4.8
                // https://github.com/getsentry/sentry-dotnet/issues/954

                // Can fail on IL2CPP with an unclear line number and this is an optional information:
                // ArgumentOutOfRangeException: The added or subtracted value results in an un-representable DateTime.
                // https://github.com/getsentry/sentry-unity/issues/233

                _options.LogError(
                    "Failed to find BootTime: Now {0}, GetTimestamp {1}, Frequency {2}, TicksPerSecond: {3}",
                    e,
                    now,
                    timestamp,
                    Stopwatch.Frequency,
                    TimeSpan.TicksPerSecond);
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
                        _options.LogError("Failure getting precise App startup time.", e);
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

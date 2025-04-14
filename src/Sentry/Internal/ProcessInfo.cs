using Sentry.Extensibility;

namespace Sentry.Internal;

internal class ProcessInfo
{
    internal static ProcessInfo? Instance;

    /// <summary>
    /// The timespan.GetTimestamp() value at init
    /// More precise for determining TTID
    /// </summary>
    internal long StartupTimestamp { get; private set; } = 0L;

    /// <summary>
    /// When the code was initialized.
    /// </summary>
    internal DateTimeOffset? StartupTime { get; private set; }

    /// <summary>
    /// When the device was initialized.
    /// </summary>
    internal DateTimeOffset? BootTime { get; }

    private volatile Task _preciseAppStartupTask = Task.CompletedTask;
    private int? _id;

    // For testability
    internal Task PreciseAppStartupTask
    {
        get => _preciseAppStartupTask;
        private set => _preciseAppStartupTask = value;
    }

    public int? GetId(SentryOptions options) => _id ??= GetCurrentProcessId(options);

    private int? GetCurrentProcessId(SentryOptions options)
    {
#if NET6_0_OR_GREATER
        return Environment.ProcessId;
#else
        try
        {
            return Process.GetCurrentProcess().Id;
        }
        catch (Exception ex)
        {
            options.LogError(ex, "Error getting current process Id");
            return null;
        }
#endif
    }

    internal ProcessInfo(
        SentryOptions options,
        Func<DateTimeOffset>? findPreciseStartupTime = null)
    {
        if (options.DetectStartupTime == StartupTimeDetectionMode.None)
        {
            options.LogDebug("Not detecting startup time due to option: {0}", options.DetectStartupTime);
            return;
        }

        // Fast
        var now = DateTimeOffset.UtcNow;
        StartupTime = now;
        try
        {
            StartupTimestamp = Stopwatch.GetTimestamp();
            BootTime = now.AddTicks(-StartupTimestamp
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

            options.LogError(e,
                "Failed to find BootTime: Now {0}, GetTimestamp {1}, Frequency {2}, TicksPerSecond: {3}",
                now,
                StartupTimestamp,
                Stopwatch.Frequency, TimeSpan.TicksPerSecond);
        }

        // An opt-out to the more precise approach (mainly due to IL2CPP):
        // https://issuetracker.unity3d.com/issues/il2cpp-player-crashes-when-calling-process-dot-getcurrentprocess-dot-starttime
        if (options.DetectStartupTime == StartupTimeDetectionMode.Best)
        {
#if __MOBILE__
            options.LogWarning("StartupTimeDetectionMode.Best is not available on this platform.  Using 'Fast' mode.");
#else
            // StartupTime is set to UtcNow in this constructor.
            // That's computationally cheap but not very precise.
            // This method will give a better precision to the StartupTime at a cost
            // of calling Process.GetCurrentProcess, on a thread pool thread.
            var preciseStartupTimeFunc = findPreciseStartupTime ?? GetStartupTime;
            PreciseAppStartupTask = Task.Run(() =>
            {
                try
                {
                    StartupTime = preciseStartupTimeFunc();
                }
                catch (Exception e)
                {
                    options.LogError(e, "Failure getting precise App startup time.");
                    //Ignore any exception and stay with the less-precise DateTime.UtcNow value.
                }
            }).ContinueWith(_ =>
                // Let the actual task get collected
                PreciseAppStartupTask = Task.CompletedTask);
#endif
        }
    }

#if !__MOBILE__
    private static DateTimeOffset GetStartupTime()
    {
        using var proc = Process.GetCurrentProcess();
        return proc.StartTime.ToUniversalTime();
    }
#endif

    public bool? ApplicationIsActivated(SentryOptions options)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var activatedHandle = GetForegroundWindow();
                if (activatedHandle == IntPtr.Zero)
                {
                    return false;       // No window is currently activated
                }

                var currentProcessId = ProcessInfo.Instance?.GetId(options);
                GetWindowThreadProcessId(activatedHandle, out var activeProcessId);

                return activeProcessId == currentProcessId;
            }
        }
        catch (Exception e)
        {
            options.LogError(e, "Error getting foreground window state.");
        }
        return null;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    private static extern IntPtr GetForegroundWindow();

    // GetWindowThreadProcessId is only available in the Windows SDK, so trying to call this from UWP apps will fail.
    // We wrap use of this in a try/catch as a workaround. However, we need `ExactSpelling = true` here  to suppress
    // warnings about the use of this API when compiling UWP applications.
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
    private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);
}

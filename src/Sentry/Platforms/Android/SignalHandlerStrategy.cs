namespace Sentry.Android;

/// <summary>
/// Defines how Sentry Native's signal handler interacts with the CLR/Mono
/// signal handler.
/// </summary>
public enum SignalHandlerStrategy
{
    /// <summary>
    /// Sentry Native captures the crash first, then invokes the .NET runtime's signal
    /// handler. The runtime may convert the same signal into a managed exception (e.g.,
    /// <c>SIGSEGV</c> into <c>NullReferenceException</c>), which can result in duplicate
    /// crash reports.
    /// </summary>
    Default,
    /// <summary>
    /// Sentry Native invokes the .NET runtime's signal handler first, then captures the
    /// native crash. This avoids duplicate crash reports from both the native signal and
    /// the managed exception. This strategy is supported on Android 8.0 (API level 26)
    /// and later; on older versions, Sentry Native silently falls back to
    /// <see cref="Default"/>.
    /// </summary>
    /// <remarks>
    /// .NET runtimes 10.0.0–10.0.3 (.NET SDKs 10.0.100–10.0.301) are not compatible with
    /// this strategy. Using it on affected versions throws an
    /// <see cref="System.InvalidOperationException"/> during initialization.
    /// The issue was resolved in .NET runtime 10.0.4 (.NET SDK 10.0.400). See
    /// <see href="https://github.com/dotnet/runtime/pull/123346">dotnet/runtime#123346</see>.
    /// </remarks>
    ChainAtStart
}

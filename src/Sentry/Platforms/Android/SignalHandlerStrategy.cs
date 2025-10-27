namespace Sentry.Android;

/// <summary>
/// Defines how Sentry Native's signal handler interacts with the CLR/Mono
/// signal handler.
/// </summary>
public enum SignalHandlerStrategy
{
    /// <summary>
    /// Invokes the CLR/Mono signal handler at the end of Sentry Native's signal
    /// handler.
    /// </summary>
    Default,
    /// <summary>
    /// Invokes the CLR/Mono signal handler at the start of Sentry Native's
    /// signal handler.
    /// </summary>
    /// <remarks>
    /// .NET runtimes 10.0.100–10.0.301 shipped in .NET SDKs 10.0.0–10.0.3 crash when used
    /// with this option. The issue was fixed in
    /// <see href="https://github.com/dotnet/runtime/pull/123346">dotnet/runtime#123346</see>,
    /// which shipped as .NET runtime 10.0.400 in .NET SDK 10.0.4.
    /// </remarks>
    ChainAtStart
}

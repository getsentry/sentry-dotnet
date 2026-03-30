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
    /// .NET runtimes 10.0.0–10.0.3 (.NET SDKs 10.0.100–10.0.301) are not compatible with
    /// this strategy. On affected versions, the SDK automatically falls back to
    /// <see cref="Default"/>. The issue was resolved in .NET runtime 10.0.4
    /// (.NET SDK 10.0.400). See
    /// <see href="https://github.com/dotnet/runtime/pull/123346">dotnet/runtime#123346</see>.
    /// </remarks>
    ChainAtStart
}

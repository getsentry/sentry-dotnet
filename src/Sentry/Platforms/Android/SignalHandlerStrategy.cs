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
    ChainAtStart
}

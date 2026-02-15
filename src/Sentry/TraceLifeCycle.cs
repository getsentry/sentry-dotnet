namespace Sentry;

/// <summary>
/// Describes how spans should be sent to Sentry.
/// </summary>
public enum TraceLifeCycle
{
    /// <summary>
    /// Spans are sent in a <see cref="SentryTransaction"/>
    /// </summary>
    Static,
    /// <summary>
    /// Spans are sent streamed to Sentry as they are finished.
    /// </summary>
    Stream
}

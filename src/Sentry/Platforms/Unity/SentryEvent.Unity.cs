#if SENTRY_UNITY

namespace Sentry;

public sealed partial class SentryEvent
{
    /// <summary>
    /// Indicates whether this event was actually captured and sent to Sentry.
    /// Used by the Unity SDK's async screenshot capture to avoid sending orphaned attachments.
    /// Defaults to false (safe: if unset, attachments are skipped rather than orphaned).
    /// </summary>
    internal bool IsCaptured { get; set; }
}

#endif

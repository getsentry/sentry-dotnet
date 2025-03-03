namespace Sentry;

/// <summary>
/// Session replay quality. Higher quality means finer details
/// but a greater performance impact.
/// </summary>
/// <remarks>
/// <see href="https://docs.sentry.io/platforms/android/session-replay/performance-overhead/">Android</see>
/// <see href="https://docs.sentry.io/platforms/apple/guides/ios/session-replay/performance-overhead/">iOS</see>
/// </remarks>
public enum SentryReplayQuality
{
    /// <summary>
    /// Low replay quality.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/getsentry/sentry-java/blob/main/sentry/src/main/java/io/sentry/SentryReplayOptions.java">Android</see>
    /// <see href="https://github.com/getsentry/sentry-cocoa/blob/main/Sources/Swift/Integrations/SessionReplay/SentryReplayOptions.swift">iOS</see>
    /// </remarks>
    Low = 0,

    /// <summary>
    /// Medium replay quality.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/getsentry/sentry-java/blob/main/sentry/src/main/java/io/sentry/SentryReplayOptions.java">Android</see>
    /// <see href="https://github.com/getsentry/sentry-cocoa/blob/main/Sources/Swift/Integrations/SessionReplay/SentryReplayOptions.swift">iOS</see>
    /// </remarks>
    Medium = 1,

    /// <summary>
    /// High replay quality.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/getsentry/sentry-java/blob/main/sentry/src/main/java/io/sentry/SentryReplayOptions.java">Android</see>
    /// <see href="https://github.com/getsentry/sentry-cocoa/blob/main/Sources/Swift/Integrations/SessionReplay/SentryReplayOptions.swift">iOS</see>
    /// </remarks>
    High = 2
}

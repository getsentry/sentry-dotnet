namespace Sentry;

/// <summary>
/// Used to specify the reason why feedback capture failed, in the event of a failure
/// </summary>
public enum CaptureFeedbackErrorReason
{
    /// <summary>
    /// <para>
    /// An unknown error occurred (enable debug mode and check the logs for details).
    /// </para>
    /// <para>
    /// Possible causes:
    /// <list type="bullet">
    ///   <item>
    ///     <description>An exception from the configureScope callback</description>
    ///   </item>
    ///   <item>
    ///     <description>An error when sending the envelope</description>
    ///   </item>
    ///   <item>
    ///     <description>An attempt to send feedback while the application is shutting down</description>
    ///   </item>
    ///   <item>
    ///     <description>Something more mysterious...</description>
    ///   </item>
    /// </list>
    /// </para>
    /// </summary>
    UnknownError,
    /// <summary>
    /// Capture failed because Sentry is disabled (very likely an empty DSN was provided when initialising the SDK).
    /// </summary>
    DisabledHub,
    /// <summary>
    /// Capture failed because the <see cref="SentryFeedback.Message"/> message is empty.
    /// </summary>
    EmptyMessage,
}

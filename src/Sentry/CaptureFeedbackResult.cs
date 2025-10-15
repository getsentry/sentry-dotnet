namespace Sentry;

/// <summary>
/// The result type of the <see cref="ISentryClient.CaptureFeedback"/> method
/// </summary>
public struct CaptureFeedbackResult
{
    /// <summary>
    /// Creates a successful feedback capture result with the specified event Id.
    /// </summary>
    /// <param name="eventId"></param>
    public CaptureFeedbackResult(SentryId eventId)
    {
        if (eventId == SentryId.Empty)
        {
            throw new ArgumentException("EventId cannot be empty", nameof(eventId));
        }

        EventId = eventId;
    }

    /// <summary>
    /// Creates a failed feedback capture result with the specified error reason.
    /// </summary>
    /// <param name="errorReason"></param>
    public CaptureFeedbackResult(CaptureFeedbackErrorReason errorReason)
    {
        EventId = SentryId.Empty;
        ErrorReason = errorReason;
    }

    /// <summary>
    /// The Id of the captured feedback, if successful. <see cref="SentryId.Empty"/> if feedback capture fails.
    /// </summary>
    public SentryId EventId;

    /// <inheritdoc cref="CaptureFeedbackErrorReason"/>
    public CaptureFeedbackErrorReason? ErrorReason;

    /// <summary>
    /// Implicitly converts a <see cref="CaptureFeedbackErrorReason"/> to a <see cref="CaptureFeedbackResult"/>
    /// </summary>
    /// <param name="errorReason"></param>
    /// <returns></returns>
    public static implicit operator CaptureFeedbackResult(CaptureFeedbackErrorReason errorReason) => new(errorReason);

    /// <summary>
    /// Implicitly converts a non-empty <see cref="SentryId"/> to a <see cref="CaptureFeedbackResult"/>
    /// </summary>
    /// <returns></returns>
    public static implicit operator CaptureFeedbackResult(SentryId eventId) => new(eventId);

    /// <summary>
    /// Returns true if feedback capture was successful, false otherwise.
    /// </summary>
    public bool Succeededed => ErrorReason == null;
}

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
    ///     <description>A transmission error when sending the envelope</description>
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
    /// Sentry is disabled (very likely an empty DSN was provided when initialising the SDK).
    /// </summary>
    DisabledHub,
    /// <summary>
    /// The <see cref="SentryFeedback.Message"/> message is empty.
    /// </summary>
    EmptyMessage,
}

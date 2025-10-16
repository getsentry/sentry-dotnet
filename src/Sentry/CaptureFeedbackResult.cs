namespace Sentry;

/// <summary>
/// The result type of the <see cref="ISentryClient.CaptureFeedback"/> method
/// </summary>
public sealed class CaptureFeedbackResult
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
        ErrorReason = null;
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
    public SentryId EventId { get; private init; }

    /// <inheritdoc cref="CaptureFeedbackErrorReason"/>
    public CaptureFeedbackErrorReason? ErrorReason { get; }

    /// <summary>
    /// Returns true if feedback capture was successful, false otherwise.
    /// </summary>
    public bool Succeeded => ErrorReason == null;

    /// <summary>
    /// Determines whether the specified object is equal to the current object.
    /// </summary>
    public override bool Equals(object? obj) =>
        obj is CaptureFeedbackResult other && Equals(other);

    /// <summary>
    /// Determines whether the specified <see cref="CaptureFeedbackResult"/> is equal to the current object.
    /// </summary>
    public bool Equals(CaptureFeedbackResult? other) =>
        other is not null &&
        EventId.Equals(other.EventId) &&
        Equals(ErrorReason, other.ErrorReason);

    /// <summary>
    /// Serves as the default hash function.
    /// </summary>
    public override int GetHashCode() =>
        HashCode.Combine(EventId, ErrorReason);

    /// <summary>
    /// Determines whether two specified instances of <see cref="CaptureFeedbackResult"/> are equal.
    /// Will return true if all of the members are equal.
    /// </summary>
    public static bool operator ==(CaptureFeedbackResult? left, CaptureFeedbackResult? right) =>
        Equals(left, right);

    /// <summary>
    /// Determines whether two specified instances of <see cref="CaptureFeedbackResult"/> are not equal.
    /// Will return true if any of the members are not equal.
    /// </summary>
    public static bool operator !=(CaptureFeedbackResult? left, CaptureFeedbackResult? right) =>
        !Equals(left, right);
}

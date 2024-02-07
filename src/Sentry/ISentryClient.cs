using Sentry.Protocol.Envelopes;
using Sentry.Protocol.Metrics;

namespace Sentry;

/// <summary>
/// Sentry Client interface.
/// </summary>
public interface ISentryClient
{
    /// <summary>
    /// Whether the client is enabled or not.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Capture an envelope and queue it.
    /// </summary>
    /// <param name="envelope">The envelope.</param>
    /// <returns>true if the enveloped was queued, false otherwise.</returns>
    bool CaptureEnvelope(Envelope envelope);

    /// <summary>
    /// Capture the event
    /// </summary>
    /// <param name="evt">The event to be captured.</param>
    /// <param name="scope">An optional scope to be applied to the event.</param>
    /// <param name="hint">An optional hint providing high level context for the source of the event</param>
    /// <returns>The Id of the event.</returns>
    SentryId CaptureEvent(SentryEvent evt, Scope? scope = null, SentryHint? hint = null);

    /// <summary>
    /// Captures a user feedback.
    /// </summary>
    /// <param name="userFeedback">The user feedback to send to Sentry.</param>
    void CaptureUserFeedback(UserFeedback userFeedback);

    /// <summary>
    /// Captures a transaction.
    /// </summary>
    /// <remarks>
    /// Note: this method is NOT meant to be called from user code!
    /// Instead, call <see cref="ISpan.Finish()"/> on the transaction.
    /// </remarks>
    /// <param name="transaction">The transaction.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    void CaptureTransaction(SentryTransaction transaction);

    /// <summary>
    /// Captures a transaction.
    /// </summary>
    /// <remarks>
    /// Note: this method is NOT meant to be called from user code!
    /// Instead, call <see cref="ISpan.Finish()"/> on the transaction.
    /// </remarks>
    /// <param name="transaction">The transaction.</param>
    /// <param name="scope">The scope to be applied to the transaction</param>
    /// <param name="hint">
    /// A hint providing extra context.
    /// This will be available in callbacks prior to processing the transaction.
    /// </param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    void CaptureTransaction(SentryTransaction transaction, Scope? scope, SentryHint? hint);

    /// <summary>
    /// Captures a session update.
    /// </summary>
    /// <remarks>
    /// Note: this method is NOT meant to be called from user code!
    /// It will be called automatically by the SDK.
    /// </remarks>
    /// <param name="sessionUpdate">The update to send to Sentry.</param>
    void CaptureSession(SessionUpdate sessionUpdate);

    /// <summary>
    /// Captures a Checkin.
    /// </summary>
    /// <param name="checkin">The Checkin to send to Sentry</param>
    void CaptureCheckin(Checkin checkin);

    /// <summary>
    /// Flushes the queue of captured events until the timeout is reached.
    /// </summary>
    /// <param name="timeout">The amount of time allowed for flushing.</param>
    /// <returns>A task to await for the flush operation.</returns>
    Task FlushAsync(TimeSpan timeout);
}

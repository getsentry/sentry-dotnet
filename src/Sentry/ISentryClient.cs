using System;
using System.Threading.Tasks;

namespace Sentry
{
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
        /// Capture the event.
        /// </summary>
        /// <param name="evt">The event to be captured.</param>
        /// <param name="scope">An optional scope to be applied to the event.</param>
        /// <returns>The Id of the event.</returns>
        SentryId CaptureEvent(SentryEvent evt, Scope? scope = null);

        /// <summary>
        /// Captures a user feedback.
        /// </summary>
        /// <param name="userFeedback">The user feedback to send to Sentry.</param>
        void CaptureUserFeedback(UserFeedback userFeedback);

        /// <summary>
        /// Flushes events queued up.
        /// </summary>
        /// <param name="timeout">How long to wait for flush to finish.</param>
        /// <returns>A task to await for the flush operation.</returns>
        Task FlushAsync(TimeSpan timeout);
    }
}

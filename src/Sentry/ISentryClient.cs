using System;
using System.Threading.Tasks;
using Sentry.Protocol;

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
        /// Captures a transaction.
        /// </summary>
        /// <remarks>
        /// Note: this method is NOT meant to be called from user code!
        /// Instead, call <see cref="ISpan.Finish"/> on the transaction.
        /// </remarks>
        /// <param name="transaction">The transaction.</param>
        void CaptureTransaction(ITransaction transaction);

        /// <summary>
        /// Flushes events queued up.
        /// </summary>
        /// <param name="timeout">How long to wait for flush to finish.</param>
        /// <returns>A task to await for the flush operation.</returns>
        Task FlushAsync(TimeSpan timeout);
    }
}

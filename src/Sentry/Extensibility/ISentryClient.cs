using System;

namespace Sentry.Extensibility
{
    /// <summary>
    /// Sentry Client interface
    /// </summary>
    /// <remarks>
    /// The contract of which <see cref="T:Sentry.SentryCore" /> exposes statically.
    /// This interface exist to allow better testability of integrations which otherwise
    /// would require dependency to the static <see cref="T:Sentry.SentryCore" />
    /// </remarks>
    /// <inheritdoc />
    public interface ISentryClient : ISentryScopeManagement
    {
        /// <summary>
        /// Whether the client is enabled or not
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// Capture the event
        /// </summary>
        /// <param name="evt">The event to be captured</param>
        /// <returns>The Id of the event</returns>
        Guid CaptureEvent(SentryEvent evt);
        /// <summary>
        /// Captures the event created by the specified function
        /// </summary>
        /// <param name="eventFactory">The function which creates the event.</param>
        /// <remarks>
        /// If the client is disabled, the function is never invoked.
        /// This is especially useful if the callback executes something heavy
        /// </remarks>
        /// <returns></returns>
        Guid CaptureEvent(Func<SentryEvent> eventFactory);
    }
}

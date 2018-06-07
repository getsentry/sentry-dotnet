using System;
using Sentry.Protocol;

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
        /// <param name="scope">An optional scope to be applied to the event.</param>
        /// <returns>The Id of the event</returns>
        Guid CaptureEvent(SentryEvent evt, Scope scope = null);
    }
}

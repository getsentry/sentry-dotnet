using System;
using System.ComponentModel;
using Sentry.Protocol;

namespace Sentry
{
    /// <summary>
    /// Extension methods for <see cref="ISentryClient"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SentryClientExtensions
    {
        /// <summary>
        /// Captures the exception.
        /// </summary>
        /// <param name="client">The Sentry client.</param>
        /// <param name="ex">The exception.</param>
        /// <returns>The Id of the event</returns>
        public static Guid CaptureException(this ISentryClient client, Exception ex)
        {
            return !client.IsEnabled
                ? Guid.Empty
                : client.CaptureEvent(new SentryEvent(ex));
        }

        /// <summary>
        /// Captures a message.
        /// </summary>
        /// <param name="client">The Sentry client.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="level">The message level.</param>
        /// <returns>The Id of the event</returns>
        public static Guid CaptureMessage(
            this ISentryClient client,
            string message,
            SentryLevel level = SentryLevel.Info)
        {
            return !client.IsEnabled
                   || string.IsNullOrWhiteSpace(message)
                ? Guid.Empty
                : client.CaptureEvent(new SentryEvent
                {
                    Message = message,
                    Level = level
                });
        }
    }
}

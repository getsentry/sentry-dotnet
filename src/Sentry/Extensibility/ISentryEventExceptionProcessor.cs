using System;

namespace Sentry.Extensibility
{
    /// <summary>
    /// Process exceptions and augments the event with its data.
    /// </summary>
    public interface ISentryEventExceptionProcessor
    {
        /// <summary>
        /// Process the exception and augments the event with its data.
        /// </summary>
        /// <param name="exception">The exception to process.</param>
        /// <param name="sentryEvent">The event to add data to.</param>
        void Process(Exception exception, SentryEvent sentryEvent);
    }
}

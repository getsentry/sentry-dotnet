using System;

namespace Sentry.Extensibility
{
    /// <summary>
    /// Process an exception type and augments the event with its data.
    /// </summary>
    /// <typeparam name="TException">The type of the exception to process.</typeparam>
    /// <inheritdoc />
    public abstract class SentryEventExceptionProcessor<TException>
        : ISentryEventExceptionProcessor
        where TException : Exception
    {
        /// <inheritdoc />
        public void Process(Exception? exception, SentryEvent sentryEvent)
        {
            if (exception is TException specificException)
            {
                ProcessException(specificException, sentryEvent);
            }
        }

        /// <summary>
        /// Process the exception and event.
        /// </summary>
        /// <param name="exception">The exception to process.</param>
        /// <param name="sentryEvent">The event to process.</param>
        protected internal abstract void ProcessException(TException exception, SentryEvent sentryEvent);
    }
}

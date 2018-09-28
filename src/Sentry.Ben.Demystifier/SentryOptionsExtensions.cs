using System.ComponentModel;
using Sentry.Internal;

namespace Sentry.Ben.Demystifier
{
    ///
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SentryOptionsExtensions
    {
        /// <summary>
        /// Add an exception processor
        /// </summary>
        /// <param name="options">The SentryOptions to hold the processor.</param>
        public static void UseEnhancedStackTrace(this SentryOptions options)
        {
            var sentryStackTraceFactory = new AsyncStackTraceFactory(options);

            options.EventProcessors = options.EventProcessors.RemoveAt(1)
                .Insert(1, new MainSentryEventProcessor(options, sentryStackTraceFactory));

            options.ExceptionProcessors = options.ExceptionProcessors.RemoveAt(0)
                .Insert(0, new MainExceptionProcessor(options, sentryStackTraceFactory));
        }
    }
}

using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging
{
    /// <summary>
    /// SentryLoggingOptions extensions.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SentryLoggingOptionsExtensions
    {
        /// <summary>
        /// Add an log event filter
        /// </summary>
        /// <remarks>
        /// Filters are called before sending an event.
        /// This allows the filter to decide whether the log message should not be recorded at all.
        /// </remarks>
        /// <param name="options">The <see cref="SentryLoggingOptions"/> to hold the filter.</param>
        /// <param name="filter">The filter.</param>
        public static void AddLogEntryFilter(this SentryLoggingOptions options, ILogEntryFilter filter)
            => options.Filters = options.Filters.Concat(new[] { filter }).ToArray();

        /// <summary>
        /// Add an log event filter
        /// </summary>
        /// <remarks>
        /// Filters are called before sending an event.
        /// This allows the filter to decide whether the log message should not be recorded at all.
        /// </remarks>
        /// <param name="options">The <see cref="SentryLoggingOptions"/> to hold the filter.</param>
        /// <param name="filter">The filter.</param>
        public static void AddLogEntryFilter(
            this SentryLoggingOptions options,
            Func<string, LogLevel, EventId, Exception?, bool> filter)
            => options.AddLogEntryFilter(new DelegateLogEntryFilter(filter));
    }
}

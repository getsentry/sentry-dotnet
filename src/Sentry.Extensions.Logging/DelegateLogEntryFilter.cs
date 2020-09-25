using System;
using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging
{
    /// <summary>
    /// An implementation of <see cref="T:Sentry.Extensions.Logging.ILogEntryFilter" /> that invokes a <see cref="T:System.Func`1" />
    /// </summary>
    /// <inheritdoc />
    public class DelegateLogEntryFilter : ILogEntryFilter
    {
        private readonly Func<string, LogLevel, EventId, Exception?, bool> _filter;

        /// <summary>
        /// Creates a new instance of <see cref="DelegateLogEntryFilter"/>
        /// </summary>
        /// <param name="filter"></param>
        public DelegateLogEntryFilter(Func<string, LogLevel, EventId, Exception?, bool> filter)
            => _filter = filter ?? throw new ArgumentNullException(nameof(filter));

        /// <inheritdoc />
        public bool Filter(
            string categoryName,
            LogLevel logLevel,
            EventId eventId,
            Exception? exception)
            => _filter(
                categoryName,
                logLevel,
                eventId,
                exception);
    }
}

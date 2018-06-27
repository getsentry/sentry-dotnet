using System;
using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging
{
    public class DelegateLogEventFilter : ILogEventFilter
    {
        private readonly Func<string, LogLevel, EventId, Exception, bool> _filter;

        public DelegateLogEventFilter(
            Func<string, LogLevel, EventId, Exception, bool> filter)
            => _filter = filter ?? throw new ArgumentNullException(nameof(filter));

        public bool Filter(
            string categoryName,
            LogLevel logLevel,
            EventId eventId,
            Exception exception)
            => _filter(
                categoryName,
                logLevel,
                eventId,
                exception);
    }
}

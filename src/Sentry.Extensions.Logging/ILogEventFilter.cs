using System;
using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging
{
    public interface ILogEventFilter
    {
        bool Filter(
            string categoryName,
            LogLevel logLevel,
            EventId eventId,
            Exception exception);
    }
}

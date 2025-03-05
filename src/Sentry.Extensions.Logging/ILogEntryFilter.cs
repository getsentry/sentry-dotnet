using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging;

/// <summary>
/// An abstraction to filter log entries before they reach <see cref="SentryLogger"/>
/// </summary>
public interface ILogEntryFilter
{
    /// <summary>
    /// Whether a log entry should be filtered out or not.
    /// </summary>
    /// <remarks>
    /// Before processing a log entry, <see cref="SentryLogger"/> invokes all registered <see cref="ILogEntryFilter"/>
    /// giving the application an opportunity to avoid recoding the log entry as a breadcrumb or sending an event.
    /// </remarks>
    /// <param name="categoryName">The logger category name.</param>
    /// <param name="logLevel">The event level.</param>
    /// <param name="eventId">The EventId.</param>
    /// <param name="exception">The Exception, if any.</param>
    public bool Filter(
        string categoryName,
        LogLevel logLevel,
        EventId eventId,
        Exception? exception);
}

using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging;

/// <summary>
/// SentryLoggingOptions extensions.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryLoggingOptionsExtensions
{
    /// <summary>
    /// Add a log event filter.
    /// </summary>
    /// <remarks>
    /// Filters are called before sending an event.
    /// This allows the filter to decide whether the log message should not be recorded at all.
    /// The log entry is neither captured as a <see cref="SentryEvent"/> nor added as a <see cref="Breadcrumb"/> when any filter returns <see langword="true"/>.
    /// </remarks>
    /// <param name="options">The <see cref="SentryLoggingOptions"/> to hold the filter.</param>
    /// <param name="filter">The <see cref="ILogEntryFilter"/> filter.</param>
    public static void AddLogEntryFilter(this SentryLoggingOptions options, ILogEntryFilter filter)
        => options.Filters = options.Filters.Concat(new[] { filter }).ToArray();

    /// <summary>
    /// Add a log event filter.
    /// </summary>
    /// <remarks>
    /// Filters are called before sending an event.
    /// This allows the filter to decide whether the log message should not be recorded at all.
    /// The log entry is neither captured as a <see cref="SentryEvent"/> nor added as a <see cref="Breadcrumb"/> when any filter returns <see langword="true"/>.
    /// </remarks>
    /// <param name="options">The <see cref="SentryLoggingOptions"/> to hold the filter.</param>
    /// <param name="filter">The filter <see langword="delegate"/>. Return <see langword="true"/> if the log entry should be filtered out.</param>
    public static void AddLogEntryFilter(
        this SentryLoggingOptions options,
        Func<string, LogLevel, EventId, Exception?, bool> filter)
        => options.AddLogEntryFilter(new DelegateLogEntryFilter(filter));
}

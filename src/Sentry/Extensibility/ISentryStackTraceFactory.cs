namespace Sentry.Extensibility;

/// <summary>
/// Factory to <see cref="SentryStackTrace" /> from an <see cref="Exception" />.
/// </summary>
public interface ISentryStackTraceFactory
{
    /// <summary>
    /// Creates a <see cref="SentryStackTrace" /> from the optional <see cref="Exception" />.
    /// </summary>
    /// <param name="exception">The exception to create the stacktrace from.</param>
    /// <returns>A Sentry stack trace.</returns>
    public SentryStackTrace? Create(Exception? exception = null);
}

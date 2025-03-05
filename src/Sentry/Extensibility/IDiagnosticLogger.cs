namespace Sentry.Extensibility;

/// <summary>
/// Abstraction for internal logging.
/// </summary>
public interface IDiagnosticLogger
{
    /// <summary>
    /// Whether the logger is enabled or not to the specified <see cref="SentryLevel"/>.
    /// </summary>
    public bool IsEnabled(SentryLevel level);

    /// <summary>
    /// Log an internal SDK message.
    /// </summary>
    /// <param name="logLevel">The level.</param>
    /// <param name="message">The message.</param>
    /// <param name="exception">An optional Exception.</param>
    /// <param name="args">Optional arguments for string template.</param>
    public void Log(SentryLevel logLevel, string message, Exception? exception = null, params object?[] args);
}

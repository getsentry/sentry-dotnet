using Sentry.Extensibility;

namespace Sentry.Infrastructure;

/// <summary>
/// Logger that allows logs to be sent to multiple destinations.
/// </summary>
/// <remarks>
/// If we want to make this class public, we have to have a think about how we want to handle multiple Diagnostic
/// loggers with different levels. One solution would be to throw an ArgumentException in that case.
/// </remarks>
internal class CompositeLogger : IDiagnosticLogger
{
    private readonly DiagnosticLogger[] _loggers;

    /// <summary>
    /// Creates a new instance of <see cref="CompositeLogger"/>.
    /// </summary>
    public CompositeLogger(params DiagnosticLogger[] loggers)
    {
        _loggers = loggers;
    }

    /// <inheritdoc />
    public bool IsEnabled(SentryLevel level)
    {
        foreach (var logger in _loggers)
        {
            if (logger.IsEnabled(level))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public void Log(SentryLevel logLevel, string message, Exception? exception = null, params object?[] args)
    {
        foreach (var logger in _loggers)
        {
            logger.Log(logLevel, message, exception, args);
        }
    }
}

using System;

namespace Sentry.Infrastructure;

/// <summary>
/// Console logger used by the SDK to report its internal logging.
/// </summary>
/// <remarks>
/// The default logger, usually replaced by a higher level logging adapter like Microsoft.Extensions.Logging.
/// </remarks>
public class ConsoleDiagnosticLogger : DiagnosticLogger
{
    /// <summary>
    /// Creates a new instance of <see cref="ConsoleDiagnosticLogger"/>.
    /// </summary>
    public ConsoleDiagnosticLogger(SentryLevel minimalLevel) : base(minimalLevel)
    {
    }

    /// <inheritdoc />
    protected override void LogMessage(string message) => Console.WriteLine(message);
}

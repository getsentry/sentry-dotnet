namespace Sentry.Infrastructure;

/// <summary>
/// Logger that logs to both Console and Trace outputs, used in MAUI applications by default, since Visual Studio
/// only show's Trace outputs and Rider shows Console outputs.
/// </summary>
public class ConsoleAndTraceDiagnosticLogger : DiagnosticLogger
{
    /// <summary>
    /// Creates a new instance of <see cref="ConsoleAndTraceDiagnosticLogger"/>.
    /// </summary>
    public ConsoleAndTraceDiagnosticLogger(SentryLevel minimalLevel) : base(minimalLevel)
    {
    }

    /// <inheritdoc />
    protected override void LogMessage(string message)
    {
        Console.WriteLine(message);
        Trace.WriteLine(message);
    }
}

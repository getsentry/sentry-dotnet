#define TRACE
namespace Sentry.Infrastructure;

/// <summary>
/// Trace logger used by the SDK to report its internal logging.
/// </summary>
/// <remarks>
/// Logger available when hooked to an IDE. It's useful when debugging apps running under IIS which have no output to Console logger.
/// </remarks>
public class TraceDiagnosticLogger : DiagnosticLogger
{
    /// <summary>
    /// Creates a new instance of <see cref="TraceDiagnosticLogger"/>.
    /// </summary>
    public TraceDiagnosticLogger(SentryLevel minimalLevel) : base(minimalLevel)
    {
    }

    /// <inheritdoc />
    protected override void LogMessage(string message) => Trace.WriteLine(message);
}

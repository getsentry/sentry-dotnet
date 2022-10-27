using System.Diagnostics;

namespace Sentry.Infrastructure;

/// <summary>
/// Debug logger used by the SDK to report its internal logging.
/// </summary>
/// <remarks>
/// Logger available when compiled in Debug mode. It's useful when debugging apps running under IIS which have no output to Console logger.
/// </remarks>
[Obsolete("Logger doesn't work outside of Sentry SDK. Please use TraceDiagnosticLogger instead")]
public class DebugDiagnosticLogger : DiagnosticLogger
{
    /// <summary>
    /// Creates a new instance of <see cref="DebugDiagnosticLogger"/>.
    /// </summary>
    public DebugDiagnosticLogger(SentryLevel minimalLevel) : base(minimalLevel)
    {
    }

    /// <inheritdoc />
    protected override void LogMessage(string message) => Debug.WriteLine(message);
}

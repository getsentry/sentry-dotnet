using System;
using System.Diagnostics;
using Sentry.Extensibility;

namespace Sentry.Infrastructure
{
    /// <summary>
    /// Debug logger used by the SDK to report its internal logging.
    /// </summary>
    /// <remarks>
    /// Logger available when compiled in Debug mode. It's useful when debugging apps running under IIS which have no output to Console logger.
    /// </remarks>
    public class DebugDiagnosticLogger : IDiagnosticLogger
    {
        private readonly SentryLevel _minimalLevel;

        /// <summary>
        /// Creates a new instance of <see cref="DebugDiagnosticLogger"/>.
        /// </summary>
        public DebugDiagnosticLogger(SentryLevel minimalLevel) => _minimalLevel = minimalLevel;

        /// <summary>
        /// Whether the logger is enabled to the defined level.
        /// </summary>
        public bool IsEnabled(SentryLevel level) => level >= _minimalLevel;

        /// <summary>
        /// Log message with level, exception and parameters.
        /// </summary>
        public void Log(SentryLevel logLevel, string message, Exception? exception = null, params object?[] args)
            => Debug.Write($@"{logLevel,7}: {string.Format(message, args)}
{exception}");
    }
}

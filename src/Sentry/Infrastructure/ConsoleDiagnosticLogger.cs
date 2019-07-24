using System;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Infrastructure
{
    /// <summary>
    /// Console logger used by the SDK to report its internal logging
    /// </summary>
    /// <remarks>
    /// The default logger, usually replaced by a higher level logging adapter like Microsoft.Extensions.Logging
    /// </remarks>
    public class ConsoleDiagnosticLogger : IDiagnosticLogger
    {
        private readonly SentryLevel _minimalLevel;

        /// <summary>
        /// Creates a new instance of <see cref="ConsoleDiagnosticLogger"/>.
        /// </summary>
        /// <param name="minimalLevel"></param>
        public ConsoleDiagnosticLogger(SentryLevel minimalLevel) => _minimalLevel = minimalLevel;

        /// <summary>
        /// Whether the logger is enabled to the defined level.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public bool IsEnabled(SentryLevel level) => level >= _minimalLevel;

        /// <summary>
        /// Log message with level, exception and parameters
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        /// <param name="args"></param>
        public void Log(SentryLevel logLevel, string message, Exception exception = null, params object[] args)
            => Console.Write($@"{logLevel,7}: {string.Format(message, args)}
{exception}");
    }
}

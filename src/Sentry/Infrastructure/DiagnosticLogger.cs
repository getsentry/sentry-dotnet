using System;
using Sentry.Extensibility;

namespace Sentry.Infrastructure
{
    public abstract class DiagnosticLogger : IDiagnosticLogger
    {
        private readonly SentryLevel _minimalLevel;

        /// <summary>
        /// Creates a new instance of <see cref="DiagnosticLogger"/>.
        /// </summary>
        protected DiagnosticLogger(SentryLevel minimalLevel) => _minimalLevel = minimalLevel;

        /// <summary>
        /// Whether the logger is enabled to the defined level.
        /// </summary>
        public bool IsEnabled(SentryLevel level) => level >= _minimalLevel;

        /// <summary>
        /// Log message with level, exception and parameters.
        /// </summary>
        public void Log(SentryLevel logLevel, string message, Exception? exception = null, params object?[] args)
        {
            // Note, newlines are removed to guard against log injection attacks.
            // See https://github.com/getsentry/sentry-dotnet/security/code-scanning/5

            var formattedMessage = string.Format(message, args)
                .Replace(Environment.NewLine, "");

            var completeMessage = exception == null
                ? $"{logLevel,7}: {formattedMessage}"
                : $"{logLevel,7}: {formattedMessage}\n{exception}";

            LogMessage(completeMessage);
        }

        protected abstract void LogMessage(string message);
    }
}

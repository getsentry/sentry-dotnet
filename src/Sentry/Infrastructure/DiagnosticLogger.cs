using System;
using System.Text;
using Sentry.Extensibility;

namespace Sentry.Infrastructure
{
    /// <summary>
    /// Base class for diagnostic loggers.
    /// </summary>
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
            // Note, linefeed and newline chars are removed to guard against log injection attacks.
            // See https://github.com/getsentry/sentry-dotnet/security/code-scanning/5

            // Important: Only format the string if there are args passed.
            // Otherwise, a pre-formatted string that contains braces can cause a FormatException.
            var text = args.Length == 0 ? message : string.Format(message, args);
            var formattedMessage = ScrubNewlines(text);

            var completeMessage = exception == null
                ? $"{logLevel,7}: {formattedMessage}"
                : $"{logLevel,7}: {formattedMessage}{Environment.NewLine}{exception}";

            LogMessage(completeMessage);
        }

        /// <summary>
        /// Writes a formatted message to the log.
        /// </summary>
        /// <param name="message">The complete message, ready to be logged.</param>
        protected abstract void LogMessage(string message);

        private static string ScrubNewlines(string s)
        {
            // Replaces "\r", "\n", or "\r\n" with a single space in one pass (and trims the end result)

            var sb = new StringBuilder(s.Length);

            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                switch (c)
                {
                    case '\r':
                        sb.Append(' ');
                        if (i < s.Length - 1 && s[i + 1] == '\n')
                        {
                            i++; // to prevent two consecutive spaces from "\r\n"
                        }
                        break;
                    case '\n':
                        sb.Append(' ');
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            // trim end and return
            var len = sb.Length;
            while (sb[len - 1] == ' ')
            {
                len--;
            }

            return sb.ToString(0, len);
        }
    }
}

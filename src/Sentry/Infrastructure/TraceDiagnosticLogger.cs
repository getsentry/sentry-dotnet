using System;
using System.Diagnostics;
using Sentry.Extensibility;
namespace Sentry.Infrastructure
{
    /// <summary>
    /// Trace logger used by the SDK to report its internal logging.
    /// </summary>
    /// <remarks>
    /// Logger available when hooked to an IDE. It's useful when debugging apps running under IIS which have no output to Console logger.
    /// </remarks>
    public class TraceDiagnosticLogger : IDiagnosticLogger
    {
        private readonly SentryLevel _minimalLevel;

        /// <summary>
        /// Creates a new instance of <see cref="TraceDiagnosticLogger"/>.
        /// </summary>
        public TraceDiagnosticLogger(SentryLevel minimalLevel) => _minimalLevel = minimalLevel;

        /// <summary>
        /// Whether the logger is enabled to the defined level.
        /// </summary>
        public bool IsEnabled(SentryLevel level) => level >= _minimalLevel;

        /// <summary>
        /// Log message with level, exception and parameters.
        /// </summary>
        public void Log(SentryLevel logLevel, string message, Exception? exception = null, params object?[] args)
        {
            try
            {
                for (var i = 0; i < Trace.Listeners.Count; i++)
                {
                    var listener = Trace.Listeners[i];
                    listener.WriteLine($"{logLevel,7}: {string.Format(message, args)}");
                    if (exception != null)
                    {
                        listener.WriteLine(exception);
                    }
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // Trace.Listeners is a public mutable static property and listeners can be removed
                // from anywhere at anytime.
            }
        }
    }
}

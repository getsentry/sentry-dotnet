using System;
using System.Diagnostics;
using Sentry.Extensibility;
using System.Linq;

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

        private Lazy<TraceListener?> _traceListener = new Lazy<TraceListener?>(() => GetFirstOrDefaultListener());

        /// <summary>
        /// Get the first TraceListener, usually it's the process debug output.
        /// </summary>
        /// <returns>The first TraceListener, if available</returns>
        private static TraceListener? GetFirstOrDefaultListener()
        {
            var listeners = Trace.Listeners;
            return listeners.Count > 0 ? listeners[0] : null;
        }

        /// <summary>
        /// Creates a new instance of <see cref="TraceDiagnosticLogger"/>.
        /// </summary>
        public TraceDiagnosticLogger(SentryLevel minimalLevel) => _minimalLevel = minimalLevel;

        /// <summary>
        /// Whether the logger is enabled to the defined level.
        /// </summary>
        public bool IsEnabled(SentryLevel level) => level >= _minimalLevel && _traceListener.Value != null;

        /// <summary>
        /// Log message with level, exception and parameters.
        /// </summary>
        public void Log(SentryLevel logLevel, string message, Exception? exception = null, params object?[] args)
            => _traceListener.Value?.Write($@"{logLevel,7}: {string.Format(message, args)}
{exception}");
    }
}

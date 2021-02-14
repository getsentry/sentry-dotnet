using System;
using Microsoft.Extensions.Logging;
using Sentry.Extensibility;

namespace Sentry.Extensions.Logging
{
    /// <summary>
    /// MEL => Microsoft.Extensions.Logging
    /// </summary>
    /// <remarks>
    /// Replaces the default Console logger as early as the logging factory is built
    /// </remarks>
    public class MelDiagnosticLogger : IDiagnosticLogger
    {
        private readonly ILogger<ISentryClient> _logger;
        private readonly SentryLevel _level;

        /// <summary>
        /// Creates a new instance of <see cref="MelDiagnosticLogger"/>.
        /// </summary>
        public MelDiagnosticLogger(ILogger<ISentryClient> logger, SentryLevel level)
        {
            _logger = logger;
            _level = level;
        }

        /// <summary>
        /// Whether this logger is enabled for the provided level
        /// </summary>
        /// <remarks>
        /// Enabled if the level is equal or higher than then both <see cref="SentryLevel"/>
        /// set via the options and also the inner <see cref="ILogger{TCategoryName}"/>
        /// </remarks>
        /// <param name="level"></param>
        /// <returns></returns>
        public bool IsEnabled(SentryLevel level) => _logger.IsEnabled(level.ToMicrosoft()) && level >= _level;

        /// <summary>
        /// Logs the message.
        /// </summary>
        public void Log(SentryLevel logLevel, string message, Exception? exception = null, params object?[] args)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            _logger.Log(logLevel.ToMicrosoft(), exception, message, args);
        }
    }
}

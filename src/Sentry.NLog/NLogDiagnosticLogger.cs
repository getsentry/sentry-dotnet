using System;
using Sentry.Extensibility;

using NLog.Common;
using Sentry.Infrastructure;

namespace Sentry.NLog
{
    internal class NLogDiagnosticLogger : IDiagnosticLogger
    {
        private readonly IDiagnosticLogger? _extraLogger;

        public NLogDiagnosticLogger(IDiagnosticLogger? extraLogger = null)
        {
            if (!InternalLogger.LogToConsole || !(extraLogger is ConsoleDiagnosticLogger))
            {
                _extraLogger = extraLogger;
            }
        }

        public bool IsEnabled(SentryLevel level)
        {
            if (_extraLogger?.IsEnabled(level) == true)
            {
                return true;
            }

            return level switch
            {
                SentryLevel.Fatal => InternalLogger.IsFatalEnabled,
                SentryLevel.Error => InternalLogger.IsErrorEnabled,
                SentryLevel.Warning => InternalLogger.IsWarnEnabled,
                SentryLevel.Info => InternalLogger.IsInfoEnabled,
                _ => InternalLogger.IsDebugEnabled
            };
        }

        public void Log(SentryLevel logLevel, string message, Exception? exception = null, params object?[] args)
        {
            switch (logLevel)
            {
                case SentryLevel.Fatal:
                    InternalLogger.Fatal(exception, message, args);
                    break;
                case SentryLevel.Error:
                    InternalLogger.Error(exception, message, args);
                    break;
                case SentryLevel.Warning:
                    InternalLogger.Warn(exception, message, args);
                    break;
                case SentryLevel.Info:
                    InternalLogger.Info(exception, message, args);
                    break;
                default:
                    InternalLogger.Debug(exception, message, args);
                    break;
            }

            if (_extraLogger?.IsEnabled(logLevel) == true)
            {
                _extraLogger.Log(logLevel, message, exception, args);
            }
        }
    }
}

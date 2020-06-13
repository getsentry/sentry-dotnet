using System;
using Sentry.Extensibility;
using Sentry.Protocol;

using NLog.Common;
using Sentry.Infrastructure;

namespace Sentry.NLog
{
    internal class NLogDiagnosticLogger : IDiagnosticLogger
    {
        private readonly IDiagnosticLogger _extraLogger;

        public NLogDiagnosticLogger(IDiagnosticLogger extraLogger = null)
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

            switch (level)
            {
                case SentryLevel.Fatal: return InternalLogger.IsFatalEnabled;
                case SentryLevel.Error: return InternalLogger.IsErrorEnabled;
                case SentryLevel.Warning: return InternalLogger.IsWarnEnabled;
                case SentryLevel.Info: return InternalLogger.IsInfoEnabled;
                default: return InternalLogger.IsDebugEnabled;
            }
        }

        public void Log(SentryLevel logLevel, string message, Exception exception = null, params object[] args)
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

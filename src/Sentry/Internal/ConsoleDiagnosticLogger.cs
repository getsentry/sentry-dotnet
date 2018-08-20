using System;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internal
{
    internal class ConsoleDiagnosticLogger : IDiagnosticLogger
    {
        private readonly SentryLevel _minimalLevel;

        public ConsoleDiagnosticLogger(SentryLevel minimalLevel) => _minimalLevel = minimalLevel;

        public bool IsEnabled(SentryLevel level) => level >= _minimalLevel;

        public void Log(SentryLevel logLevel, string message, Exception exception = null, params object[] args)
            => Console.Write($@"{logLevel,7}: {string.Format(message, args)}
{exception}");
    }
}

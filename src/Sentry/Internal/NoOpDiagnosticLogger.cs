using System;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internal
{
    internal class NoOpDiagnosticLogger : IDiagnosticLogger
    {
        public bool IsEnabled(SentryLevel level) => false;

        public void Log(SentryLevel logLevel, string message, Exception exception = null, params object[] args)
        {
        }
    }
}

using System;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Internal
{
    internal class NoOpDiagnosticLogger : IDiagnosticLogger
    {
        public void Log(SentryLevel logLevel, string message, Exception exception)
        { }
    }
}

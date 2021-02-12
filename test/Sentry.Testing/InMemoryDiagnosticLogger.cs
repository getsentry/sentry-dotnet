using System;
using System.Collections.Generic;
using Sentry.Extensibility;

namespace Sentry.Testing
{
    public class InMemoryDiagnosticLogger : IDiagnosticLogger
    {
        public List<Entry> Entries { get; } = new();

        public bool IsEnabled(SentryLevel level) => true;

        public void Log(SentryLevel logLevel, string message, Exception exception = null, params object[] args)
        {
            Entries.Add(new Entry(logLevel, message, exception, args));
        }

        public record Entry(
            SentryLevel Level,
            string Message,
            Exception Exception,
            object[] Args
        );
    }
}

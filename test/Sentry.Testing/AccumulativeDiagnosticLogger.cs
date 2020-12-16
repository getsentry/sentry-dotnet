using System;
using System.Collections.Generic;
using Sentry.Extensibility;

namespace Sentry.Testing
{
    public class AccumulativeDiagnosticLogger : IDiagnosticLogger
    {
        public List<Entry> Entries { get; } = new();

        public bool IsEnabled(SentryLevel level) => true;

        public void Log(SentryLevel logLevel, string message, Exception exception = null, params object[] args)
        {
            Entries.Add(new Entry(logLevel, message, exception, args));
        }

        public class Entry
        {
            public SentryLevel Level { get; }

            public string Message { get; }

            public Exception Exception { get; }

            public object[] Args { get; }

            public Entry(SentryLevel level, string message, Exception exception, object[] args)
            {
                Level = level;
                Message = message;
                Exception = exception;
                Args = args;
            }
        }
    }
}

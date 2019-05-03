using System;
using Sentry.Protocol;

namespace Sentry.Extensibility
{
    public interface ISentryStackTraceFactory
    {
        SentryStackTrace Create(Exception exception = null);
    }
}

using System;
using Sentry.Protocol;

namespace Sentry.Internal
{
    internal interface ISentryStackTraceFactory
    {
        SentryStackTrace Create(Exception exception = null);
    }
}
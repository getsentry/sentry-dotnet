#if !WINDOWS_UWP
using System;
using System.Collections.Generic;
using System.Text;

namespace Sentry.Infrastructure
{
    internal static class DefaultDiagnosticLogger
    {
        public static ConsoleDiagnosticLogger Get(SentryLevel minimalLevel) => new ConsoleDiagnosticLogger(minimalLevel);
    }
}
#endif

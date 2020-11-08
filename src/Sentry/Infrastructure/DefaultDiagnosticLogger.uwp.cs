#if WINDOWS_UWP
namespace Sentry.Infrastructure
{
    internal static class DefaultDiagnosticLogger
    {
        public static DebugDiagnosticLogger Get(SentryLevel minimalLevel) => new DebugDiagnosticLogger(minimalLevel);
    }
}
#endif

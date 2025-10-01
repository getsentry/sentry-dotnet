namespace Sentry.EntityFramework;

/// <summary>
/// Sentry Database Logger
/// </summary>
internal static class SentryDatabaseLogging
{
#if NET9_0_OR_GREATER
    private static bool _init;

    const bool TRUE = true;
    const bool FALSE = false;
#else
    private static int _init;

    const int TRUE = 1;
    const int FALSE = 0;
#endif

    internal static SentryCommandInterceptor? UseBreadcrumbs(
        IQueryLogger? queryLogger = null,
        bool initOnce = true,
        IDiagnosticLogger? diagnosticLogger = null)
    {
        if (initOnce && Interlocked.Exchange(ref _init, TRUE) != FALSE)
        {
            diagnosticLogger?.LogWarning("{0}.{1} was already executed.",
                nameof(SentryDatabaseLogging), nameof(UseBreadcrumbs));
            return null;
        }

        diagnosticLogger?.LogInfo("{0}.{1} adding interceptor.",
            nameof(SentryDatabaseLogging), nameof(UseBreadcrumbs));

        queryLogger ??= new SentryQueryLogger();
        var interceptor = new SentryCommandInterceptor(queryLogger);
        DbInterception.Add(interceptor);
        return interceptor;
    }
}

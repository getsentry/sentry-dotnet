namespace Sentry.EntityFramework;

/// <summary>
/// Sentry Database Logger
/// </summary>
internal static class SentryDatabaseLogging
{
    private static InterlockedBoolean _init;

    internal static SentryCommandInterceptor? UseBreadcrumbs(
        IQueryLogger? queryLogger = null,
        bool initOnce = true,
        IDiagnosticLogger? diagnosticLogger = null)
    {
        if (initOnce && _init.Exchange(true))
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

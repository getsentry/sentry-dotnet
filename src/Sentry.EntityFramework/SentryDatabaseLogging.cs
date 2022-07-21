using System.Data.Entity.Infrastructure.Interception;
using Sentry.Extensibility;

namespace Sentry.EntityFramework;

/// <summary>
/// Sentry Database Logger
/// </summary>
public static class SentryDatabaseLogging
{
    private static int Init;

    /// <summary>
    /// Adds an instance of <see cref="SentryCommandInterceptor"/> to <see cref="DbInterception"/>
    /// This is a static setup call, so make sure you only call it once for each <see cref="IQueryLogger"/> instance you want to register globally
    /// </summary>
    /// <param name="logger">Query Logger.</param>
    [Obsolete("This method is called automatically by options.AddEntityFramework. This method will be removed in future versions.")]
    public static SentryCommandInterceptor? UseBreadcrumbs(IQueryLogger? logger = null)
        => UseBreadcrumbs(logger, true);

    internal static SentryCommandInterceptor? UseBreadcrumbs(
        IQueryLogger? queryLogger = null,
        bool initOnce = true,
        IDiagnosticLogger? diagnosticLogger = null)
    {
        if (initOnce && Interlocked.Exchange(ref Init, 1) != 0)
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

namespace Sentry.EntityFramework;

internal static class DbCommandInterceptionContextExtensions
{
    internal static ISpan? GetSpanFromContext<T>(this DbCommandInterceptionContext<T> interceptionContext)
        => interceptionContext.FindUserState(SentryQueryPerformanceListener.SentryUserStateKey) as ISpan;

    internal static void AttachSpan<T>(this DbCommandInterceptionContext<T> interceptionContext, ISpan span)
        => interceptionContext.SetUserState(SentryQueryPerformanceListener.SentryUserStateKey, span);
}

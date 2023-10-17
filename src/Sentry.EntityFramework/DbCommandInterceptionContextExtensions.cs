namespace Sentry.EntityFramework;

internal static class DbCommandInterceptionContextExtensions
{
    internal static ISpanTracer? GetSpanFromContext<T>(this DbCommandInterceptionContext<T> interceptionContext)
        => interceptionContext.FindUserState(SentryQueryPerformanceListener.SentryUserStateKey) as ISpanTracer;

    internal static void AttachSpan<T>(this DbCommandInterceptionContext<T> interceptionContext, ISpanTracer span)
        => interceptionContext.SetUserState(SentryQueryPerformanceListener.SentryUserStateKey, span);
}

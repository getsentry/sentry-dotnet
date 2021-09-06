using System.Data.Entity.Infrastructure.Interception;

namespace Sentry.EntityFramework.Internals.Extensions
{
    internal static class DbCommandInterceptionContextExtensions
    {
        internal static ISpan? GetSpanFromContext<T>(this DbCommandInterceptionContext<T> interceptionContext)
        {
#if !NET461
            if (interceptionContext.FindUserState(SentryQueryPerformanceListener.SentryUserStateKey) is ISpan span)
            {
                return span;
            }
#endif
            return null;
        }

        internal static void AttachSpan<T>(this DbCommandInterceptionContext<T> interceptionContext, ISpan span)
        {
#if !NET461
            interceptionContext.SetUserState(SentryQueryPerformanceListener.SentryUserStateKey, span);
#endif
        }
    }
}

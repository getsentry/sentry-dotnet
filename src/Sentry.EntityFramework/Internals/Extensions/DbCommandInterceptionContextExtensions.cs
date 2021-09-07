using System.Data.Entity.Infrastructure.Interception;

namespace Sentry.EntityFramework.Internals.Extensions
{
    internal static class DbCommandInterceptionContextExtensions
    {
        internal static ISpan? GetSpanFromContext<T>(this DbCommandInterceptionContext<T> interceptionContext)
        {
            if (interceptionContext.FindUserState(SentryQueryPerformanceListener.SentryUserStateKey) is ISpan span)
            {
                return span;
            }
            return null;
        }

        internal static void AttachSpan<T>(this DbCommandInterceptionContext<T> interceptionContext, ISpan span)
            => interceptionContext.SetUserState(SentryQueryPerformanceListener.SentryUserStateKey, span);
    }
}

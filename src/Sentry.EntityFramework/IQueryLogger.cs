using Sentry.Protocol;

namespace Sentry.EntityFramework
{
    public interface IQueryLogger
    {
        void Log(string text, BreadcrumbLevel level = BreadcrumbLevel.Debug);
    }
}

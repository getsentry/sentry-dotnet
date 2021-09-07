using System.Data.Entity.Infrastructure.Interception;
using Sentry.Integrations;

namespace Sentry.EntityFramework
{
    class DbInterceptionIntegration : ISdkIntegration
    {
        private IDbInterceptor? SqlInterceptor { get; set; }

        public void Register(IHub hub, SentryOptions options)
        {
            SqlInterceptor = new SentryQueryPerformanceListener(hub, options);
            DbInterception.Add(SqlInterceptor);
        }

        public void Unregister(IHub _) => DbInterception.Remove(SqlInterceptor);
    }
}

using System.Data.Entity.Infrastructure.Interception;
using Sentry.Integrations;

namespace Sentry.EntityFramework;

internal class DbInterceptionIntegration : ISdkIntegration
{
    private IDbInterceptor? _sqlInterceptor { get; set; }

    public void Register(IHub hub, SentryOptions options)
    {
        _sqlInterceptor = new SentryQueryPerformanceListener(hub, options);
        DbInterception.Add(_sqlInterceptor);
    }

    public void Unregister() => DbInterception.Remove(_sqlInterceptor);
}

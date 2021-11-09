using Sentry.Tests;

namespace Sentry.Tunnel.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryTunnelMiddleware).Assembly.CheckApproval();
    }
}

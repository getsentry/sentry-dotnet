using Sentry.Tests;
using VerifyXunit;
using Xunit;

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

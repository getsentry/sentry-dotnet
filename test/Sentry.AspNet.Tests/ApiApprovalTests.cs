using Sentry.AspNet;
using Sentry.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryAspNetOptionsExtensions).Assembly.CheckApproval();
    }
}

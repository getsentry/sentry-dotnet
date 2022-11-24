namespace Sentry.AspNet.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryAspNetOptionsExtensions).Assembly.CheckApproval();
    }
}

namespace Sentry.AspNet.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryAspNetOptionsExtensions).Assembly.CheckApproval();
    }
}

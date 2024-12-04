namespace Sentry.AspNetCore.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryAspNetCoreBuilder).Assembly.CheckApproval();
    }
}

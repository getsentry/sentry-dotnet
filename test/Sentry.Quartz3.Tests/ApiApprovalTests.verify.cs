namespace Sentry.Quartz.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryJobListener).Assembly.CheckApproval();
    }
}

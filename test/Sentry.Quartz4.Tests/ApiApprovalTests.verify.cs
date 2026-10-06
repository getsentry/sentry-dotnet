namespace Sentry.Quartz.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryQuartzBuilderExtensions).Assembly.CheckApproval();
    }
}

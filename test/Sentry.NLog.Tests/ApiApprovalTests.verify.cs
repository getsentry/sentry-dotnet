namespace Sentry.NLog.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryTarget).Assembly.CheckApproval();
    }
}

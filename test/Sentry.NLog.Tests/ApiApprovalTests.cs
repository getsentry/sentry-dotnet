namespace Sentry.NLog.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryTarget).Assembly.CheckApproval();
    }
}

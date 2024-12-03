namespace Sentry.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentrySdk).Assembly.CheckApproval();
    }
}

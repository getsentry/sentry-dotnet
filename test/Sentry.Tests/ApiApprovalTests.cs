namespace Sentry.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    [Trait("Category", "Verify")]
    public Task Run()
    {
        return typeof(SentrySdk).Assembly.CheckApproval();
    }
}

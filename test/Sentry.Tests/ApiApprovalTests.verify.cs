namespace Sentry.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    [UniqueForAot]
    public Task Run()
    {
        return typeof(SentrySdk).Assembly.CheckApproval();
    }
}

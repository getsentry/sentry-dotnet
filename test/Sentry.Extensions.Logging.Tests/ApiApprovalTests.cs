namespace Sentry.Extensions.Logging.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    [Trait("Category", "Verify")]
    public Task Run()
    {
        return typeof(SentryLogger).Assembly.CheckApproval();
    }
}

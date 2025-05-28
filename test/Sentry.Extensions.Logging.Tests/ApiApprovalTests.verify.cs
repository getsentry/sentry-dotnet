namespace Sentry.Extensions.Logging.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryLogger).Assembly.CheckApproval();
    }
}

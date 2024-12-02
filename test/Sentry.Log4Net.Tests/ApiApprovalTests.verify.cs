namespace Sentry.Log4Net.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryAppender).Assembly.CheckApproval();
    }
}

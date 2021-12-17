using Sentry.Tests;

namespace Sentry.Log4Net.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryAppender).Assembly.CheckApproval();
    }
}

using Sentry.Tests;

namespace Sentry.Extensions.Logging.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryLogger).Assembly.CheckApproval();
    }
}

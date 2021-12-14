using Sentry.Tests;

namespace Sentry.EntityFramework.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryDatabaseLogging).Assembly.CheckApproval();
    }
}

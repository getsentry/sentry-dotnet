namespace Sentry.EntityFramework.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryDatabaseLogging).Assembly.CheckApproval();
    }
}

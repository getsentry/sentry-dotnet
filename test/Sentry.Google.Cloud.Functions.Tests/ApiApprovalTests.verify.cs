using Google.Cloud.Functions.Framework;

namespace Sentry.Google.Cloud.Functions.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryStartup).Assembly.CheckApproval();
    }
}

using Google.Cloud.Functions.Framework;
using Sentry.Tests;

namespace Sentry.Google.Cloud.Functions.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryStartup).Assembly.CheckApproval();
    }
}

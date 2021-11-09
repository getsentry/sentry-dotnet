using Sentry.Tests;
using VerifyXunit;
using Xunit;

namespace Sentry.AspNetCore.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryAspNetCoreBuilder).Assembly.CheckApproval();
    }
}

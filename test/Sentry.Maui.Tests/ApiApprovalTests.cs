using Sentry.Tests;

namespace Sentry.Maui.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    [Trait("Category", "Verify")]
    public Task Run()
    {
        return typeof(SentryMauiOptions).Assembly.CheckApproval();
    }
}

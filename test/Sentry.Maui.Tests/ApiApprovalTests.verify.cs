namespace Sentry.Maui.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryMauiOptions).Assembly.CheckApproval();
    }
}

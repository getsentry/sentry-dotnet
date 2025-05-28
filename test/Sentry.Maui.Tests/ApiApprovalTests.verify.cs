namespace Sentry.Maui.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryMauiOptions).Assembly.CheckApproval();
    }
}

namespace Sentry.Extensions.Hosting.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryGenericHostOptions).Assembly.CheckApproval();
    }
}

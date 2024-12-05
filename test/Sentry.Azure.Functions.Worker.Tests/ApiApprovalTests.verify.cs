namespace Sentry.Azure.Functions.Worker.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryFunctionsWorkerMiddleware).Assembly.CheckApproval();
    }
}

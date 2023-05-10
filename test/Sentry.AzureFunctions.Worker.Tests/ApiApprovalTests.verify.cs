namespace Sentry.AzureFunctions.Worker.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryFunctionsWorkerMiddleware).Assembly.CheckApproval();
    }
}

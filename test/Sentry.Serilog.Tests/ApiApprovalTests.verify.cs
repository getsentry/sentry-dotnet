namespace Sentry.Serilog.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentrySink).Assembly.CheckApproval();
    }
}

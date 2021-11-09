using Sentry.Tests;

namespace Sentry.Serilog.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentrySink).Assembly.CheckApproval();
    }
}

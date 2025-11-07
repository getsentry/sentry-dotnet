using Microsoft.Extensions.AI;

namespace Sentry.Extensions.AI.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryAIExtensions).Assembly.CheckApproval();
    }
}

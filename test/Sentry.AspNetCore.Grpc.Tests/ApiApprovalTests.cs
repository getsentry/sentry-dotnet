using Sentry.Tests;

namespace Sentry.AspNetCore.Grpc.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryGrpcInterceptor).Assembly.CheckApproval();
    }
}

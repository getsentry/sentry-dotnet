namespace Sentry.AspNetCore.Grpc.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryGrpcInterceptor).Assembly.CheckApproval();
    }
}

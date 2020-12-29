using System;
using System.Threading.Tasks;
using Grpc.Core;
using Xunit;

namespace Sentry.AspNetCore.Grpc.Tests
{
    [Collection(nameof(SentrySdkCollection))]
    public class IntegrationTests : SentryGrpcSdkTestFixture
    {
        [Fact]
        public async Task UnhandledException_AvailableThroughLastExceptionFilter()
        {
            var expectedException = new Exception("test");

            var handler = new GrpcRequestHandler<TestRequest, TestResponse>
            {
                Method = TestService.Descriptor.FindMethodByName("TestThrow"),
                Handler = (_, _) => throw expectedException
            };

            GrpcHandlers = new[] { handler };
            Build();

            var request = new TestRequest();
            var client = new TestService.TestServiceClient(Channel);

            _ = await Assert.ThrowsAsync<RpcException>(async () => await client.TestThrowAsync(request));

            Assert.Same(expectedException, SentryGrpcTestInterceptor.LastException);
        }
    }
}

using System.Threading.Tasks;
using Grpc.Core;

namespace Sentry.AspNetCore.Grpc.Tests
{
    public class GrpcTestService : TestService.TestServiceBase
    {
        public override Task<TestResponse> Test(TestRequest request, ServerCallContext context)
        {
            return Task.FromResult<TestResponse>(null);
        }

        public override Task<TestResponse> TestThrow(TestRequest request, ServerCallContext context)
        {
            return Task.FromResult<TestResponse>(null);
        }
    }
}

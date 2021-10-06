using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace Sentry.Tests
{
    [UsesVerify]
    public class ApiApprovalTests
    {
        [Fact]
        public Task Run() => typeof(SentrySdk).Assembly.CheckApproval();
    }
}

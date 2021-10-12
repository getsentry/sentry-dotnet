using System.Threading.Tasks;
using Sentry.Tests;
using VerifyXunit;
using Xunit;

namespace Sentry.Log4Net.Tests
{
    [UsesVerify]
    public class ApiApprovalTests
    {
        [Fact]
        public Task Run()
        {
            return typeof(SentryAppender).Assembly.CheckApproval();
        }
    }
}

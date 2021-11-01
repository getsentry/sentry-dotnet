using System.Threading.Tasks;
using Google.Cloud.Functions.Framework;
using Sentry.Tests;
using VerifyXunit;
using Xunit;

namespace Sentry.Google.Cloud.Functions.Tests
{
    [UsesVerify]
    public class ApiApprovalTests
    {
        [Fact]
        public Task Run()
        {
            return typeof(SentryStartup).Assembly.CheckApproval();
        }
    }
}

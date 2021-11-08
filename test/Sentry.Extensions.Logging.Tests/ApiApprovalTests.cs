using System.Threading.Tasks;
using Sentry.Tests;
using VerifyXunit;
using Xunit;

namespace Sentry.Extensions.Logging.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryLogger).Assembly.CheckApproval();
    }
}

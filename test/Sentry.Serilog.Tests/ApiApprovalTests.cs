using System.Threading.Tasks;
using Sentry.Tests;
using VerifyXunit;
using Xunit;

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

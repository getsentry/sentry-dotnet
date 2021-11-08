using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace Sentry.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentrySdk).Assembly.CheckApproval();
    }
}
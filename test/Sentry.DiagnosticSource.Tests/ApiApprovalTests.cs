using System.Threading.Tasks;
using Sentry.Internals.DiagnosticSource;
using Sentry.Tests;
using VerifyXunit;
using Xunit;

namespace Sentry.DiagnosticSource.Tests
{
    [UsesVerify]
    public class ApiApprovalTests
    {
        [Fact]
        public Task Run()
        {
            return typeof(SentryDiagnosticListenerIntegration).Assembly.CheckApproval();
        }
    }
}

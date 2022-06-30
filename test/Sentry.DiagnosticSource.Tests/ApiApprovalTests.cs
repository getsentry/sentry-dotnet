using Sentry.Internals.DiagnosticSource;
using Sentry.Tests;

namespace Sentry.DiagnosticSource.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryDiagnosticListenerIntegration).Assembly.CheckApproval();
    }
}

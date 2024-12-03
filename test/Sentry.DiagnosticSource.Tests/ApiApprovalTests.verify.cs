// Only generate/test API approvals for targets where DiagnosticSource isn't integrated with the Sentry assembly
#if !NETCOREAPP3_1_OR_GREATER
using Sentry.Internal.DiagnosticSource;

namespace Sentry.DiagnosticSource.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryDiagnosticListenerIntegration).Assembly.CheckApproval();
    }
}
#endif

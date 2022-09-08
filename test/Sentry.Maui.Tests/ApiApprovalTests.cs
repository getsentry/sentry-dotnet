#if !__MOBILE__
using Sentry.Tests;

namespace Sentry.Maui.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(SentryMauiOptions).Assembly.CheckApproval();
    }
}
#endif

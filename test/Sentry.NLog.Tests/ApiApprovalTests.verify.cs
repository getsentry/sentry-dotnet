namespace Sentry.NLog.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [SkippableFact]
    public Task Run()
    {
        return typeof(SentryTarget).Assembly.CheckApproval();
    }
}

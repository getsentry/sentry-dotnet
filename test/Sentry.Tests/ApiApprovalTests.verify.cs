namespace Sentry.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [SkippableFact()]
    public Task Run()
    {
        // Skip this test in the feat/v4.0.0 branch
        var assembly = AppDomain.CurrentDomain.GetAssemblies().
            SingleOrDefault(assembly => assembly.GetName().Name == "Sentry");
        var version = assembly.GetVersion();
        Skip.If(version.StartsWith("3"));

        return typeof(SentrySdk).Assembly.CheckApproval();
    }
}

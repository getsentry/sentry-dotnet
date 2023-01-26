namespace Sentry.Android.AssemblyReader.Tests;

[UsesVerify]
public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(AndroidAssemblyReader).Assembly.CheckApproval();
    }
}

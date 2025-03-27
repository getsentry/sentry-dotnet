namespace Sentry.Android.AssemblyReader.Tests;

public class ApiApprovalTests
{
    [Fact]
    public Task Run()
    {
        return typeof(AndroidAssemblyReader).Assembly.CheckApproval();
    }
}

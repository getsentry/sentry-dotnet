namespace Sentry.Tests.Internals;

public class SentrySdkNamespacesTests
{
    [Theory]
    [InlineData("Sentry")]
    [InlineData("Sentry.ISentryClient")]
    [InlineData("Sentry.AspNetCore")]
    [InlineData("Sentry.AspNetCore.SentryMiddleware")]
    [InlineData("Sentry.AspNetCore.RequestDecompression.RequestDecompressionMiddleware")]
    [InlineData("Sentry.Extensions.Logging.MelDiagnosticLogger")]
    [InlineData("Sentry.Serilog.SentrySink")]
    [InlineData("Sentry.Maui.SentryMauiOptions")]
    public void IsSentrySdk_SdkLoggerName_True(string loggerName)
        => SentrySdkNamespaces.IsSentrySdk(loggerName).Should().BeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("MyApp.Program")]
    [InlineData("Microsoft.AspNetCore.Hosting.Diagnostics")]
    // Logs from our samples shouldn't be filtered
    [InlineData("Sentry.Samples.AspNetCore.Serilog.Program")]
    [InlineData("Sentry.Samples.Console.Basic")]
    [InlineData("Sentry.Some.Class")]
    [InlineData("Sentry.MyApp.Worker")]
    // Starts with Sentry but not one of our SDK namespaces
    [InlineData("SentrySomething")]
    // A package root must match on a namespace boundary, not just as a string prefix.
    [InlineData("Sentry.MauiSomething.Thing")]
    [InlineData("Sentry.AspNetCoreExtras.Thing")]
    public void IsSentrySdk_NotAnSdkLoggerName_False(string loggerName)
        => SentrySdkNamespaces.IsSentrySdk(loggerName).Should().BeFalse();

    [Fact]
    public void PackageRoots_GeneratedFromCraftManifest_ExcludesCoreAndCoversIntegrations()
    {
        // Guards the .craft.yml parsing in the GenerateSentrySdkNamespaces target: a silently empty
        // or malformed list would stop the integrations recognising the SDK's own log messages.
        SentrySdkNamespaces.PackageRoots.Should()
            .NotBeEmpty()
            .And.OnlyContain(root => root.StartsWith("Sentry.", StringComparison.Ordinal))
            .And.Contain("Sentry.AspNetCore")
            .And.Contain("Sentry.Extensions.Logging")
            .And.Contain("Sentry.Serilog")
            // 'Sentry' itself cannot be a prefix root - see SentrySdkNamespaces.CoreNames.
            .And.NotContain("Sentry");
    }
}

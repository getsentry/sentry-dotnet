namespace Sentry.Tests;

public class BuiltInSystemDiagnosticsMetersTests
{
    [Fact]
    public void MicrosoftAspNetCoreHosting_ExactString_Matches()
    {
        var pattern = BuiltInSystemDiagnosticsMeters.MicrosoftAspNetCoreHosting;

        pattern.ShouldMatchOnlyExactText("Microsoft.AspNetCore.Hosting");
    }

    [Fact]
    public void MicrosoftAspNetCoreRouting_ExactString_Matches()
    {
        var pattern = BuiltInSystemDiagnosticsMeters.MicrosoftAspNetCoreRouting;

        pattern.ShouldMatchOnlyExactText("Microsoft.AspNetCore.Routing");
    }

    [Fact]
    public void MicrosoftAspNetCoreDiagnostics_ExactString_Matches()
    {
        var pattern = BuiltInSystemDiagnosticsMeters.MicrosoftAspNetCoreDiagnostics;

        pattern.ShouldMatchOnlyExactText("Microsoft.AspNetCore.Diagnostics");
    }

    [Fact]
    public void MicrosoftAspNetCoreRateLimiting_ExactString_Matches()
    {
        var pattern = BuiltInSystemDiagnosticsMeters.MicrosoftAspNetCoreRateLimiting;

        pattern.ShouldMatchOnlyExactText("Microsoft.AspNetCore.RateLimiting");
    }

    [Fact]
    public void MicrosoftAspNetCoreHeaderParsing_ExactString_Matches()
    {
        var pattern = BuiltInSystemDiagnosticsMeters.MicrosoftAspNetCoreHeaderParsing;

        pattern.ShouldMatchOnlyExactText("Microsoft.AspNetCore.HeaderParsing");
    }

    [Fact]
    public void MicrosoftAspNetCoreServerKestrel_ExactString_Matches()
    {
        var pattern = BuiltInSystemDiagnosticsMeters.MicrosoftAspNetCoreServerKestrel;

        pattern.ShouldMatchOnlyExactText("Microsoft.AspNetCore.Server.Kestrel");
    }

    [Fact]
    public void MicrosoftAspNetCoreHttpConnections_ExactString_Matches()
    {
        var pattern = BuiltInSystemDiagnosticsMeters.MicrosoftAspNetCoreHttpConnections;

        pattern.ShouldMatchOnlyExactText("Microsoft.AspNetCore.Http.Connections");
    }

    [Fact]
    public void MicrosoftExtensionsDiagnosticsHealthChecks_ExactString_Matches()
    {
        var pattern = BuiltInSystemDiagnosticsMeters.MicrosoftExtensionsDiagnosticsHealthChecks;

        pattern.ShouldMatchOnlyExactText("Microsoft.Extensions.Diagnostics.HealthChecks");
    }

    [Fact]
    public void MicrosoftExtensionsDiagnosticsResourceMonitoring_ExactString_Matches()
    {
        var pattern = BuiltInSystemDiagnosticsMeters.MicrosoftExtensionsDiagnosticsResourceMonitoring;

        pattern.ShouldMatchOnlyExactText("Microsoft.Extensions.Diagnostics.ResourceMonitoring");
    }

    [Fact]
    public void SystemNetNameResolution_ExactString_Matches()
    {
        var pattern = BuiltInSystemDiagnosticsMeters.SystemNetNameResolution;

        pattern.ShouldMatchOnlyExactText("System.Net.NameResolution");
    }

    [Fact]
    public void SystemNetHttp_ExactString_Matches()
    {
        var pattern = BuiltInSystemDiagnosticsMeters.SystemNetHttp;

        pattern.ShouldMatchOnlyExactText("System.Net.Http");
    }
}

internal static class BuiltInSystemDiagnosticsMetersTestsExtensions
{
    internal static void ShouldMatchOnlyExactText(this StringOrRegex pattern, string actual)
    {
        var withPrefix = "prefix" + actual;
        var withSuffix = actual + "suffix";
        SubstringOrPatternMatcher.Default.IsMatch(pattern, actual).Should().BeTrue();
        SubstringOrPatternMatcher.Default.IsMatch(pattern, withPrefix).Should().BeFalse();
        SubstringOrPatternMatcher.Default.IsMatch(pattern, withSuffix).Should().BeFalse();
    }
}

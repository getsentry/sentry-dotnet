using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Settings for the experimental Metrics feature. This feature is preview only and will very likely change in the future
/// without a major version bump... so use at your own risk.
/// </summary>
public class ExperimentalMetricsOptions
{
    /// <summary>
    /// Determines whether code locations should be recorded for Metrics
    /// </summary>
    public bool EnableCodeLocations { get; set; } = true;

    private IList<SubstringOrRegexPattern> _captureSystemDiagnosticsInstruments = new List<SubstringOrRegexPattern>();

    /// <summary>
    /// <para>
    /// A list of Substrings or Regular Expressions. Any `System.Diagnostics.Metrics.Instrument` whose name
    /// matches one of the items in this list will be collected and reported to Sentry.
    /// </para>
    /// <para>
    /// These can be either custom Instruments that you have created or any of the built in metrics that are available.
    /// </para>
    /// <para>
    /// See https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics for more information.
    /// </para>
    /// </summary>
    public IList<SubstringOrRegexPattern> CaptureSystemDiagnosticsInstruments
    {
        // NOTE: During configuration binding, .NET 6 and lower used to just call Add on the existing item.
        //       .NET 7 changed this to call the setter with an array that already starts with the old value.
        //       We have to handle both cases.
        get => _captureSystemDiagnosticsInstruments;
        set => _captureSystemDiagnosticsInstruments = value.WithConfigBinding();
    }

    private IList<SubstringOrRegexPattern> _captureSystemDiagnosticsMeters = BuiltInSystemDiagnosticsMeters.All;

    /// <summary>
    /// <para>
    /// A list of Substrings or Regular Expressions. Instruments for any `System.Diagnostics.Metrics.Meter`
    /// whose name matches one of the items in this list will be collected and reported to Sentry.
    /// </para>
    /// <para>
    /// These can be either custom Instruments that you have created or any of the built in metrics that are available.
    /// </para>
    /// <para>
    /// See https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics for more information.
    /// </para>
    /// </summary>
    public IList<SubstringOrRegexPattern> CaptureSystemDiagnosticsMeters
    {
        // NOTE: During configuration binding, .NET 6 and lower used to just call Add on the existing item.
        //       .NET 7 changed this to call the setter with an array that already starts with the old value.
        //       We have to handle both cases.
        get => _captureSystemDiagnosticsMeters;
        set => _captureSystemDiagnosticsMeters = value.WithConfigBinding();
    }
}

/// <summary>
/// Well known values for built in metrics that can be configured for
/// <see cref="ExperimentalMetricsOptions.CaptureSystemDiagnosticsMeters"/>
/// </summary>
public static partial class BuiltInSystemDiagnosticsMeters
{
    private const string MicrosoftAspNetCoreHostingPattern = @"^Microsoft\.AspNetCore\.Hosting$";
    private const string MicrosoftAspNetCoreRoutingPattern = @"^Microsoft\.AspNetCore\.Routing$";
    private const string MicrosoftAspNetCoreDiagnosticsPattern = @"^Microsoft\.AspNetCore\.Diagnostics$";
    private const string MicrosoftAspNetCoreRateLimitingPattern = @"^Microsoft\.AspNetCore\.RateLimiting$";
    private const string MicrosoftAspNetCoreHeaderParsingPattern = @"^Microsoft\.AspNetCore\.HeaderParsing$";
    private const string MicrosoftAspNetCoreServerKestrelPattern = @"^Microsoft\.AspNetCore\.Server\.Kestrel$";
    private const string MicrosoftAspNetCoreHttpConnectionsPattern = @"^Microsoft\.AspNetCore\.Http\.Connections$";
    private const string MicrosoftExtensionsDiagnosticsHealthChecksPattern = @"^Microsoft\.Extensions\.Diagnostics\.HealthChecks$";
    private const string MicrosoftExtensionsDiagnosticsResourceMonitoringPattern = @"^Microsoft\.Extensions\.Diagnostics\.ResourceMonitoring$";
    private const string SystemNetNameResolutionPattern = @"^System\.Net\.NameResolution$";
    private const string SystemNetHttpPattern = @"^System\.Net\.Http$";

    /// <summary>
    /// Matches the built in Microsoft.AspNetCore.Hosting metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly SubstringOrRegexPattern MicrosoftAspNetCoreHosting = MicrosoftAspNetCoreHostingRegex();

    [GeneratedRegex(MicrosoftAspNetCoreHostingPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftAspNetCoreHostingRegex();
#else
    public static readonly SubstringOrRegexPattern MicrosoftAspNetCoreHosting = new Regex(MicrosoftAspNetCoreHostingPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built in Microsoft.AspNetCore.Routing metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly SubstringOrRegexPattern MicrosoftAspNetCoreRouting = MicrosoftAspNetCoreRoutingRegex();

    [GeneratedRegex(MicrosoftAspNetCoreRoutingPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftAspNetCoreRoutingRegex();
#else
    public static readonly SubstringOrRegexPattern MicrosoftAspNetCoreRouting = new Regex(MicrosoftAspNetCoreRoutingPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built in Microsoft.AspNetCore.Diagnostics metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly SubstringOrRegexPattern MicrosoftAspNetCoreDiagnostics = MicrosoftAspNetCoreDiagnosticsRegex();

    [GeneratedRegex(MicrosoftAspNetCoreDiagnosticsPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftAspNetCoreDiagnosticsRegex();
#else
    public static readonly SubstringOrRegexPattern MicrosoftAspNetCoreDiagnostics = new Regex(MicrosoftAspNetCoreDiagnosticsPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built in Microsoft.AspNetCore.RateLimiting metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly SubstringOrRegexPattern MicrosoftAspNetCoreRateLimiting = MicrosoftAspNetCoreRateLimitingRegex();

    [GeneratedRegex(MicrosoftAspNetCoreRateLimitingPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftAspNetCoreRateLimitingRegex();
#else
    public static readonly SubstringOrRegexPattern MicrosoftAspNetCoreRateLimiting = new Regex(MicrosoftAspNetCoreRateLimitingPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built in Microsoft.AspNetCore.HeaderParsing metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly SubstringOrRegexPattern MicrosoftAspNetCoreHeaderParsing = MicrosoftAspNetCoreHeaderParsingRegex();

    [GeneratedRegex(MicrosoftAspNetCoreHeaderParsingPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftAspNetCoreHeaderParsingRegex();
#else
    public static readonly SubstringOrRegexPattern MicrosoftAspNetCoreHeaderParsing = new Regex(MicrosoftAspNetCoreHeaderParsingPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built in Microsoft.AspNetCore.Server.Kestrel metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly SubstringOrRegexPattern MicrosoftAspNetCoreServerKestrel = MicrosoftAspNetCoreServerKestrelRegex();

    [GeneratedRegex(MicrosoftAspNetCoreServerKestrelPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftAspNetCoreServerKestrelRegex();
#else
    public static readonly SubstringOrRegexPattern MicrosoftAspNetCoreServerKestrel = new Regex(MicrosoftAspNetCoreServerKestrelPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built in Microsoft.AspNetCore.Http.Connections metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly SubstringOrRegexPattern MicrosoftAspNetCoreHttpConnections = MicrosoftAspNetCoreHttpConnectionsRegex();

    [GeneratedRegex(MicrosoftAspNetCoreHttpConnectionsPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftAspNetCoreHttpConnectionsRegex();
#else
    public static readonly SubstringOrRegexPattern MicrosoftAspNetCoreHttpConnections = new Regex(MicrosoftAspNetCoreHttpConnectionsPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built in Microsoft.Extensions.Diagnostics.HealthChecks metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly SubstringOrRegexPattern MicrosoftExtensionsDiagnosticsHealthChecks = MicrosoftExtensionsDiagnosticsHealthChecksRegex();

    [GeneratedRegex(MicrosoftExtensionsDiagnosticsHealthChecksPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftExtensionsDiagnosticsHealthChecksRegex();
#else
    public static readonly SubstringOrRegexPattern MicrosoftExtensionsDiagnosticsHealthChecks = new Regex(MicrosoftExtensionsDiagnosticsHealthChecksPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built in Microsoft.Extensions.Diagnostics.ResourceMonitoring metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly SubstringOrRegexPattern MicrosoftExtensionsDiagnosticsResourceMonitoring = MicrosoftExtensionsDiagnosticsResourceMonitoringRegex();

    [GeneratedRegex(MicrosoftExtensionsDiagnosticsResourceMonitoringPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftExtensionsDiagnosticsResourceMonitoringRegex();
#else
    public static readonly SubstringOrRegexPattern MicrosoftExtensionsDiagnosticsResourceMonitoring = new Regex(MicrosoftExtensionsDiagnosticsResourceMonitoringPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built in System.Net.NameResolution metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly SubstringOrRegexPattern SystemNetNameResolution = SystemNetNameResolutionRegex();

    [GeneratedRegex(SystemNetNameResolutionPattern, RegexOptions.Compiled)]
    private static partial Regex SystemNetNameResolutionRegex();
#else
    public static readonly SubstringOrRegexPattern SystemNetNameResolution = new Regex(SystemNetNameResolutionPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built in <see cref="System.Net.Http"/> metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly SubstringOrRegexPattern SystemNetHttp = SystemNetHttpRegex();

    [GeneratedRegex(SystemNetHttpPattern, RegexOptions.Compiled)]
    private static partial Regex SystemNetHttpRegex();
#else
    public static readonly SubstringOrRegexPattern SystemNetHttp = new Regex(SystemNetHttpPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches all built in metrics
    /// </summary>
    /// <returns></returns>
    public static IList<SubstringOrRegexPattern> All = [
        MicrosoftAspNetCoreHosting,
        MicrosoftAspNetCoreRouting,
        MicrosoftAspNetCoreDiagnostics,
        MicrosoftAspNetCoreRateLimiting,
        MicrosoftAspNetCoreHeaderParsing,
        MicrosoftAspNetCoreServerKestrel,
        MicrosoftAspNetCoreHttpConnections,
        SystemNetNameResolution,
        SystemNetHttp,
        MicrosoftExtensionsDiagnosticsHealthChecks,
        MicrosoftExtensionsDiagnosticsResourceMonitoring
    ];
}

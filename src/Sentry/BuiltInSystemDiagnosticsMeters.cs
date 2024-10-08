using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Well known values for built-in metrics that can be configured for
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
    private const string OpenTelemetryInstrumentationRuntimePattern = @"^OpenTelemetry\.Instrumentation\.Runtime$";
    private const string SystemNetNameResolutionPattern = @"^System\.Net\.NameResolution$";
    private const string SystemNetHttpPattern = @"^System\.Net\.Http$";

    /// <summary>
    /// Matches the built-in Microsoft.AspNetCore.Hosting metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly StringOrRegex MicrosoftAspNetCoreHosting = MicrosoftAspNetCoreHostingRegex();

    [GeneratedRegex(MicrosoftAspNetCoreHostingPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftAspNetCoreHostingRegex();
#else
    public static readonly StringOrRegex MicrosoftAspNetCoreHosting = new Regex(MicrosoftAspNetCoreHostingPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built-in Microsoft.AspNetCore.Routing metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly StringOrRegex MicrosoftAspNetCoreRouting = MicrosoftAspNetCoreRoutingRegex();

    [GeneratedRegex(MicrosoftAspNetCoreRoutingPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftAspNetCoreRoutingRegex();
#else
    public static readonly StringOrRegex MicrosoftAspNetCoreRouting = new Regex(MicrosoftAspNetCoreRoutingPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built-in Microsoft.AspNetCore.Diagnostics metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly StringOrRegex MicrosoftAspNetCoreDiagnostics = MicrosoftAspNetCoreDiagnosticsRegex();

    [GeneratedRegex(MicrosoftAspNetCoreDiagnosticsPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftAspNetCoreDiagnosticsRegex();
#else
    public static readonly StringOrRegex MicrosoftAspNetCoreDiagnostics = new Regex(MicrosoftAspNetCoreDiagnosticsPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built-in Microsoft.AspNetCore.RateLimiting metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly StringOrRegex MicrosoftAspNetCoreRateLimiting = MicrosoftAspNetCoreRateLimitingRegex();

    [GeneratedRegex(MicrosoftAspNetCoreRateLimitingPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftAspNetCoreRateLimitingRegex();
#else
    public static readonly StringOrRegex MicrosoftAspNetCoreRateLimiting = new Regex(MicrosoftAspNetCoreRateLimitingPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built-in Microsoft.AspNetCore.HeaderParsing metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly StringOrRegex MicrosoftAspNetCoreHeaderParsing = MicrosoftAspNetCoreHeaderParsingRegex();

    [GeneratedRegex(MicrosoftAspNetCoreHeaderParsingPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftAspNetCoreHeaderParsingRegex();
#else
    public static readonly StringOrRegex MicrosoftAspNetCoreHeaderParsing = new Regex(MicrosoftAspNetCoreHeaderParsingPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built-in Microsoft.AspNetCore.Server.Kestrel metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly StringOrRegex MicrosoftAspNetCoreServerKestrel = MicrosoftAspNetCoreServerKestrelRegex();

    [GeneratedRegex(MicrosoftAspNetCoreServerKestrelPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftAspNetCoreServerKestrelRegex();
#else
    public static readonly StringOrRegex MicrosoftAspNetCoreServerKestrel = new Regex(MicrosoftAspNetCoreServerKestrelPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built-in Microsoft.AspNetCore.Http.Connections metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly StringOrRegex MicrosoftAspNetCoreHttpConnections = MicrosoftAspNetCoreHttpConnectionsRegex();

    [GeneratedRegex(MicrosoftAspNetCoreHttpConnectionsPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftAspNetCoreHttpConnectionsRegex();
#else
    public static readonly StringOrRegex MicrosoftAspNetCoreHttpConnections = new Regex(MicrosoftAspNetCoreHttpConnectionsPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built-in Microsoft.Extensions.Diagnostics.HealthChecks metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly StringOrRegex MicrosoftExtensionsDiagnosticsHealthChecks = MicrosoftExtensionsDiagnosticsHealthChecksRegex();

    [GeneratedRegex(MicrosoftExtensionsDiagnosticsHealthChecksPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftExtensionsDiagnosticsHealthChecksRegex();
#else
    public static readonly StringOrRegex MicrosoftExtensionsDiagnosticsHealthChecks = new Regex(MicrosoftExtensionsDiagnosticsHealthChecksPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built-in Microsoft.Extensions.Diagnostics.ResourceMonitoring metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly StringOrRegex MicrosoftExtensionsDiagnosticsResourceMonitoring = MicrosoftExtensionsDiagnosticsResourceMonitoringRegex();

    [GeneratedRegex(MicrosoftExtensionsDiagnosticsResourceMonitoringPattern, RegexOptions.Compiled)]
    private static partial Regex MicrosoftExtensionsDiagnosticsResourceMonitoringRegex();
#else
    public static readonly StringOrRegex MicrosoftExtensionsDiagnosticsResourceMonitoring = new Regex(MicrosoftExtensionsDiagnosticsResourceMonitoringPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built-in System.Net.NameResolution metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly StringOrRegex OpenTelemetryInstrumentationRuntime = OpenTelemetryInstrumentationRuntimeRegex();

    [GeneratedRegex(OpenTelemetryInstrumentationRuntimePattern, RegexOptions.Compiled)]
    private static partial Regex OpenTelemetryInstrumentationRuntimeRegex();
#else
    public static readonly StringOrRegex OpenTelemetryInstrumentationRuntime = new Regex(OpenTelemetryInstrumentationRuntimePattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built-in System.Net.NameResolution metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly StringOrRegex SystemNetNameResolution = SystemNetNameResolutionRegex();

    [GeneratedRegex(SystemNetNameResolutionPattern, RegexOptions.Compiled)]
    private static partial Regex SystemNetNameResolutionRegex();
#else
    public static readonly StringOrRegex SystemNetNameResolution = new Regex(SystemNetNameResolutionPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built-in <see cref="System.Net.Http"/> metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly StringOrRegex SystemNetHttp = SystemNetHttpRegex();

    [GeneratedRegex(SystemNetHttpPattern, RegexOptions.Compiled)]
    private static partial Regex SystemNetHttpRegex();
#else
    public static readonly StringOrRegex SystemNetHttp = new Regex(SystemNetHttpPattern, RegexOptions.Compiled);
#endif

    private static readonly Lazy<IList<StringOrRegex>> LazyAll = new(() => new List<StringOrRegex>
    {
        MicrosoftAspNetCoreHosting,
        MicrosoftAspNetCoreRouting,
        MicrosoftAspNetCoreDiagnostics,
        MicrosoftAspNetCoreRateLimiting,
        MicrosoftAspNetCoreHeaderParsing,
        MicrosoftAspNetCoreServerKestrel,
        MicrosoftAspNetCoreHttpConnections,
        MicrosoftExtensionsDiagnosticsHealthChecks,
        MicrosoftExtensionsDiagnosticsResourceMonitoring,
        OpenTelemetryInstrumentationRuntime,
        SystemNetNameResolution,
        SystemNetHttp,
    });

    /// <summary>
    /// Matches all built-in metrics
    /// </summary>
    /// <returns></returns>
    public static IList<StringOrRegex> All => LazyAll.Value;
}

namespace Sentry;

/// <summary>
/// Well known values for built in metrics that can be configured for
/// <see cref="ExperimentalMetricsOptions.CaptureSystemDiagnosticsMeters"/>
/// </summary>
public static partial class WellKnownEventCounters
{
    private const string SystemRuntimePattern = @"^System\.Runtime$";
    private const string MicrosoftAspNetCoreHostingPattern = @"^Microsoft\.AspNetCore\.Hosting$";
    private const string MicrosoftAspNetCoreHttpConnectionsPattern = @"^Microsoft\.AspNetCore\.Http\.Connections$";
    private const string MicrosoftAspNetCoreServerKestrelPattern = "^Microsoft-AspNetCore-Server-Kestrel$";
    private const string SystemNetHttpPattern = @"^System\.Net\.Http$";
    private const string SystemNetNameResolutionPattern = @"^System\.Net\.NameResolution$";
    private const string SystemNetSecurityPattern = @"^System\.Net\.Security$";
    private const string SystemNetSocketsPattern = @"^System\.Net\.Sockets$";

    /// <summary>
    /// Matches the built in Microsoft.AspNetCore.Routing metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly SubstringOrRegexPattern SystemRuntime = SystemRuntimeRegex();

    [GeneratedRegex(SystemRuntimePattern, RegexOptions.Compiled)]
    private static partial Regex SystemRuntimeRegex();
#else
    public static readonly SubstringOrRegexPattern SystemRuntime = new Regex(SystemRuntimePattern, RegexOptions.Compiled);
#endif

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
    /// Matches the built in Microsoft.Extensions.Diagnostics.HealthChecks metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly SubstringOrRegexPattern SystemNetSecurity = SystemNetSecurityRegex();

    [GeneratedRegex(SystemNetSecurityPattern, RegexOptions.Compiled)]
    private static partial Regex SystemNetSecurityRegex();
#else
    public static readonly SubstringOrRegexPattern SystemNetSecurity = new Regex(SystemNetSecurityPattern, RegexOptions.Compiled);
#endif

    /// <summary>
    /// Matches the built in Microsoft.Extensions.Diagnostics.ResourceMonitoring metrics
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly SubstringOrRegexPattern SystemNetSockets = SystemNetSocketsRegex();

    [GeneratedRegex(SystemNetSocketsPattern, RegexOptions.Compiled)]
    private static partial Regex SystemNetSocketsRegex();
#else
    public static readonly SubstringOrRegexPattern SystemNetSockets = new Regex(SystemNetSocketsPattern, RegexOptions.Compiled);
#endif
}

using Sentry.Extensibility;
using Sentry.Infrastructure;

namespace Sentry;

/// <summary>
/// The severity of the structured log.
/// <para>This API is experimental and it may change in the future.</para>
/// </summary>
/// <remarks>
/// The named constants use the value of the lowest severity number per severity level:
/// <list type="table">
///   <listheader>
///     <term>SeverityNumber</term>
///     <description>SeverityText</description>
///   </listheader>
///   <item>
///     <term>1-4</term>
///     <description>Trace</description>
///   </item>
///   <item>
///     <term>5-8</term>
///     <description>Debug</description>
///   </item>
///   <item>
///     <term>9-12</term>
///     <description>Info</description>
///   </item>
///   <item>
///     <term>13-16</term>
///     <description>Warn</description>
///   </item>
///   <item>
///     <term>17-20</term>
///     <description>Error</description>
///   </item>
///   <item>
///     <term>21-24</term>
///     <description>Fatal</description>
///   </item>
/// </list>
/// </remarks>
/// <seealso href="https://develop.sentry.dev/sdk/telemetry/logs/"/>
[Experimental(DiagnosticId.ExperimentalFeature)]
public enum SentryLogLevel
{
    /// <summary>
    /// A fine-grained debugging event.
    /// </summary>
    Trace = 1,
    /// <summary>
    /// A debugging event.
    /// </summary>
    Debug = 5,
    /// <summary>
    /// An informational event.
    /// </summary>
    Info = 9,
    /// <summary>
    /// A warning event.
    /// </summary>
    Warning = 13,
    /// <summary>
    /// An error event.
    /// </summary>
    Error = 17,
    /// <summary>
    /// A fatal error such as application or system crash.
    /// </summary>
    Fatal = 21,
}

[Experimental(DiagnosticId.ExperimentalFeature)]
internal static class SentryLogLevelExtensions
{
    internal static (string, int?) ToSeverityTextAndOptionalSeverityNumber(this SentryLogLevel level, IDiagnosticLogger? logger)
    {
        return (int)level switch
        {
            <= 0 => Underflow(level, logger),
            1 => ("trace", null),
            >= 2 and <= 4 => ("trace", (int)level),
            5 => ("debug", null),
            >= 6 and <= 8 => ("debug", (int)level),
            9 => ("info", null),
            >= 10 and <= 12 => ("info", (int)level),
            13 => ("warn", null),
            >= 14 and <= 16 => ("warn", (int)level),
            17 => ("error", null),
            >= 18 and <= 20 => ("error", (int)level),
            21 => ("fatal", null),
            >= 22 and <= 24 => ("fatal", (int)level),
            >= 25 => Overflow(level, logger),
        };
    }

    private static (string, int?) Underflow(SentryLogLevel level, IDiagnosticLogger? logger)
    {
        logger?.LogDebug("Log level {0} out of range ... clamping to minimum value {1} ({2})", level, 1, "trace");
        return ("trace", 1);
    }

    private static (string, int?) Overflow(SentryLogLevel level, IDiagnosticLogger? logger)
    {
        logger?.LogDebug("Log level {0} out of range ... clamping to maximum value {1} ({2})", level, 24, "fatal");
        return ("fatal", 24);
    }
}

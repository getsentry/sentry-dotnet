using Sentry.Extensibility;

namespace Sentry;

/// <summary>
/// The severity of the structured log.
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

        static (string, int?) Underflow(SentryLogLevel level, IDiagnosticLogger? logger)
        {
            logger?.LogDebug("Log level {0} out of range ... clamping to minimum value {1} ({2})", level, 1, "trace");
            return ("trace", 1);
        }

        static (string, int?) Overflow(SentryLogLevel level, IDiagnosticLogger? logger)
        {
            logger?.LogDebug("Log level {0} out of range ... clamping to maximum value {1} ({2})", level, 24, "fatal");
            return ("fatal", 24);
        }
    }

    internal static SentryLogLevel FromValue(int value, IDiagnosticLogger? logger)
    {
        return value switch
        {
            <= 0 => Underflow(value, logger),
            >= 1 and <= 4 => SentryLogLevel.Trace,
            >= 5 and <= 8 => SentryLogLevel.Debug,
            >= 9 and <= 12 => SentryLogLevel.Info,
            >= 13 and <= 16 => SentryLogLevel.Warning,
            >= 17 and <= 20 => SentryLogLevel.Error,
            >= 21 and <= 24 => SentryLogLevel.Fatal,
            >= 25 => Overflow(value, logger),
        };

        static SentryLogLevel Underflow(int value, IDiagnosticLogger? logger)
        {
            logger?.LogDebug("Log number {0} out of range ... clamping to minimum level {1}", value, SentryLogLevel.Trace);
            return SentryLogLevel.Trace;
        }

        static SentryLogLevel Overflow(int value, IDiagnosticLogger? logger)
        {
            logger?.LogDebug("Log number {0} out of range ... clamping to maximum level {1}", value, SentryLogLevel.Fatal);
            return SentryLogLevel.Fatal;
        }
    }

    internal static SentryLevel ToSentryLevel(this SentryLogLevel level)
    {
        return (int)level switch
        {
            <= 8 => SentryLevel.Debug,
            >= 9 and <= 12 => SentryLevel.Info,
            >= 13 and <= 16 => SentryLevel.Warning,
            >= 17 and <= 20 => SentryLevel.Error,
            >= 21 => SentryLevel.Fatal,
        };
    }
}

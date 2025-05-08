using Sentry.Infrastructure;

namespace Sentry.Protocol;

/// <summary>
/// The severity of the structured log.
/// <para>This API is experimental and it may change in the future.</para>
/// </summary>
/// <seealso href="https://develop.sentry.dev/sdk/telemetry/logs/"/>
[Experimental(DiagnosticId.ExperimentalFeature)]
public enum SentrySeverity : short
{
    /// <summary>
    /// A fine-grained debugging event.
    /// </summary>
    Trace,
    /// <summary>
    /// A debugging event.
    /// </summary>
    Debug,
    /// <summary>
    /// An informational event.
    /// </summary>
    Info,
    /// <summary>
    /// A warning event.
    /// </summary>
    Warn,
    /// <summary>
    /// An error event.
    /// </summary>
    Error,
    /// <summary>
    /// A fatal error such as application or system crash.
    /// </summary>
    Fatal,
}

[Experimental(DiagnosticId.ExperimentalFeature)]
internal static class SentrySeverityExtensions
{
    internal static string ToLogString(this SentrySeverity severity)
    {
        return severity switch
        {
            SentrySeverity.Trace => "trace",
            SentrySeverity.Debug => "debug",
            SentrySeverity.Info => "info",
            SentrySeverity.Warn => "warn",
            SentrySeverity.Error => "error",
            SentrySeverity.Fatal => "fatal",
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null),
        };
    }

    internal static SentrySeverity FromSeverityNumber(int severityNumber)
    {
        ThrowIfOutOfRange(severityNumber);

        return severityNumber switch
        {
            >= 1 and <= 4 => SentrySeverity.Trace,
            >= 5 and <= 8 => SentrySeverity.Debug,
            >= 9 and <= 12 => SentrySeverity.Info,
            >= 13 and <= 16 => SentrySeverity.Warn,
            >= 17 and <= 20 => SentrySeverity.Error,
            >= 21 and <= 24 => SentrySeverity.Fatal,
            _ => throw new UnreachableException(),
        };
    }

    internal static int ToSeverityNumber(SentrySeverity severity)
    {
        return severity switch
        {
            SentrySeverity.Trace => 1,
            SentrySeverity.Debug => 5,
            SentrySeverity.Info => 9,
            SentrySeverity.Warn => 13,
            SentrySeverity.Error => 17,
            SentrySeverity.Fatal => 21,
            _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, null)
        };
    }

    internal static void ThrowIfOutOfRange(int severityNumber)
    {
        if (severityNumber is < 1 or > 24)
        {
            throw new ArgumentOutOfRangeException(nameof(severityNumber), severityNumber, "SeverityNumber must be between 1 (inclusive) and 24 (inclusive).");
        }
    }
}

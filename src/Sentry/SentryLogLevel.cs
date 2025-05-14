using Sentry.Infrastructure;

namespace Sentry;

/// <summary>
/// The severity of the structured log.
/// <para>This API is experimental and it may change in the future.</para>
/// </summary>
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
    internal static (string, int?) ToSeverityTextAndOptionalSeverityNumber(this SentryLogLevel level)
    {
        return (int)level switch
        {
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
            _ => ThrowOutOfRange<(string, int?)>(level, nameof(level)),
        };
    }

    internal static void ThrowIfOutOfRange(SentryLogLevel level, [CallerArgumentExpression(nameof(level))] string? paramName = null)
    {
        if ((int)level is < 1 or > 24)
        {
            ThrowOutOfRange(level, paramName);
        }
    }

    [DoesNotReturn]
    private static void ThrowOutOfRange(SentryLogLevel level, string? paramName)
    {
        throw new ArgumentOutOfRangeException(paramName, level, "Severity must be between 1 (inclusive) and 24 (inclusive).");
    }

    [DoesNotReturn]
    private static T ThrowOutOfRange<T>(SentryLogLevel level, string? paramName)
    {
        throw new ArgumentOutOfRangeException(paramName, level, "Severity must be between 1 (inclusive) and 24 (inclusive).");
    }
}

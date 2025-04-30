using Sentry.Infrastructure;

namespace Sentry.Experimental;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
[Experimental(DiagnosticId.ExperimentalSentryLogs)]
public enum SentrySeverity : short
{
    Trace,
    Debug,
    Info,
    Warn,
    Error,
    Fatal,
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

[Experimental(DiagnosticId.ExperimentalSentryLogs)]
internal static class SentrySeverityExtensions
{
    public static string ToLogString(this SentrySeverity severity)
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
}

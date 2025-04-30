using Sentry.Infrastructure;

namespace Sentry.Experimental;

/// <summary>
/// Experimental Sentry SDK entrypoint.
/// </summary>
public static class SentryExperimentalSdk
{
    /// <summary>
    /// See: https://github.com/getsentry/sentry-dotnet/issues/4132
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalSentryLogs, UrlFormat = "https://github.com/getsentry/sentry-dotnet/issues/4132")]
    public static void CaptureLog(SentrySeverity level, string template, params object[]? parameters)
    {
        string message = String.Format(template, parameters ?? []);
        SentryLog log = new(level, message);
        _ = SentrySdk.CurrentHub.CaptureLog(log);
    }
}

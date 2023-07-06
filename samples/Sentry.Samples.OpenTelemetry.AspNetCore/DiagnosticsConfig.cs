using System.Diagnostics;

namespace Sentry.Samples.OpenTelemetry.AspNetCore;

public static class DiagnosticsConfig
{
    public const string ServiceName = "Sentry.Samples.OpenTelemetry.AspNetCore";
    public static ActivitySource ActivitySource { get; } = new(ServiceName);
}

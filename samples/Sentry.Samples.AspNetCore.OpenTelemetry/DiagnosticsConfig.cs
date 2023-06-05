using System.Diagnostics;

namespace Sentry.Samples.AspNetCore.OpenTelemetry;

public static class DiagnosticsConfig
{
    public const string ServiceName = "Sentry.Samples.AspNetCore.OpenTelemetry";
    public static ActivitySource ActivitySource { get; } = new(ServiceName);
}

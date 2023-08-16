using System.Diagnostics;

namespace Sentry.Samples.OpenTelemetry.AspNetCore;

public static class Telemetry
{
    public const string ServiceName = "Sentry.Samples.OpenTelemetry.AspNetCore";
    public static ActivitySource ActivitySource { get; } = new(ServiceName);
}

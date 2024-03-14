using System.Diagnostics;
using OpenTelemetry.Trace;

namespace Sentry.Samples.OpenTelemetry.AspNetCore;

public static class SampleTelemetry
{
    public const string ServiceName = "Sentry.Samples.OpenTelemetry.AspNetCore";
    public static ActivitySource ActivitySource { get; } = new(ServiceName);
    public static TracerProviderBuilder AddSampleInstrumentation(this TracerProviderBuilder builder)
        => builder.AddSource(ServiceName);
}

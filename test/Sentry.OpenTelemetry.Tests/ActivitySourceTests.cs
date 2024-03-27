using OpenTelemetry;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry.Tests.OpenTelemetry;

namespace Sentry.OpenTelemetry.Tests;

public abstract class ActivitySourceTests : IDisposable
{
    protected readonly ActivitySource Tracer;
    private readonly TracerProvider _traceProvider;

    protected ActivitySourceTests()
    {
        var activitySourceName = "ActivitySourceTests";
        var testSampler = new TestSampler();
        Tracer = new ActivitySource(activitySourceName);
        _traceProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(activitySourceName)
            .SetSampler(testSampler)
            .Build();
    }

    public void Dispose()
    {
        _traceProvider?.Dispose();
        Tracer.Dispose();
        GC.SuppressFinalize(this);
    }
}

#if NET8_0_OR_GREATER
namespace Sentry.Tests.Internals.Tracing;

/// <summary>
/// Base class for tests that need a recording ActivitySource. This is the core equivalent of
/// Sentry.OpenTelemetry.Tests.ActivitySourceTests, with the OpenTelemetry SDK's TracerProvider replaced by a
/// raw <see cref="ActivityListener"/> configured to record everything (matching the OTel default sampler used
/// in those tests).
/// </summary>
public abstract class ActivitySourceTests : IDisposable
{
    protected readonly ActivitySource Tracer;
    private readonly ActivityListener _listener;

    protected ActivitySourceTests()
    {
        // Use a unique name per test class instance so parallel tests don't listen to each other's sources.
        Tracer = new ActivitySource($"SentryActivityProcessorTests-{Guid.NewGuid()}");
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source == Tracer,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            SampleUsingParentId = static (ref ActivityCreationOptions<string> _) =>
                ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
        Tracer.Dispose();
        GC.SuppressFinalize(this);
    }
}
#endif

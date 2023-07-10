namespace Sentry.OpenTelemetry.Tests;

public abstract class ActivitySourceTests : IDisposable
{
    protected static readonly ActivitySource Tracer = new("SentrySpanProcessorTests", "1.0.0");
    private readonly ActivityListener _listener;

    protected ActivitySourceTests()
    {
        // Without a listener, activity source will not create activities
        _listener = new ActivityListener
        {
            ActivityStarted = _ => { },
            ActivityStopped = _ => { },
            ShouldListenTo = _ => true,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllDataAndRecorded,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener?.Dispose();
        GC.SuppressFinalize(this);
    }
}

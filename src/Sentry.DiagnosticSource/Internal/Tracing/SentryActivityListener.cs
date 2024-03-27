namespace Sentry.Internal.Tracing;

internal class SentryActivityListener : IDisposable
{
    private readonly ActivitySpanProcessor _activitySpanProcessor;
    private readonly ActivityListener? _listener;

    public SentryActivityListener(IHub hub) : this(new ActivitySpanProcessor(hub))
    {
    }

    public SentryActivityListener(ActivitySpanProcessor activitySpanProcessor)
    {
        _activitySpanProcessor = activitySpanProcessor;
        _listener = new ActivityListener()
        {
            // This is only for internal Sentry events
            ShouldListenTo = (source) => source.Name.StartsWith("Sentry"),
            Sample = ShouldSample,
            ActivityStarted = OnActivityStarted,
            ActivityStopped = OnActivityStopped
        };
        ActivitySource.AddActivityListener(_listener);
    }

    // We sample all the activities... these will get filtered out by the Hub when they get converted to spans
    public ActivitySamplingResult ShouldSample(ref ActivityCreationOptions<ActivityContext> _)
        => ActivitySamplingResult.AllDataAndRecorded;

    public void OnActivityStarted(System.Diagnostics.Activity activity)
    {
        _activitySpanProcessor.OnStart(activity);
    }

    public void OnActivityStopped(System.Diagnostics.Activity activity)
    {
        _activitySpanProcessor.OnEnd(activity);
    }

    public void Dispose()
    {
        _listener?.Dispose();
    }
}

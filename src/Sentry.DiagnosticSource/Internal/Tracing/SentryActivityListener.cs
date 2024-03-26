namespace Sentry.Internal.Tracing;

internal class SentryActivityListener : IDisposable
{
    private readonly ActivityListener? _listener;
    public SentryActivityListener()
    {
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
        Console.WriteLine("Started: {0,-15} {1,-60}", activity.OperationName, activity.Id);
    }

    public void OnActivityStopped(System.Diagnostics.Activity activity)
    {
        Console.WriteLine("Stopped: {0,-15} {1,-60} {2,-15}", activity.OperationName, activity.Id, activity.Duration);
    }

    public void Dispose()
    {
        _listener?.Dispose();
    }
}

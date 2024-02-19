using OpenTelemetry;

namespace Sentry.OpenTelemetry;

class DisabledSpanProcessor : BaseProcessor<Activity>
{
    private static readonly Lazy<DisabledSpanProcessor> LazyInstance = new();
    internal static DisabledSpanProcessor Instance => LazyInstance.Value;

    public DisabledSpanProcessor()
    {
        Console.WriteLine("Sentry is disabled so no OpenTelemetry spans will be sent to Sentry.");
    }

    public override void OnStart(Activity activity)
    {
        // No-Op
    }

    public override void OnEnd(Activity activity)
    {
        // No-Op
    }
}

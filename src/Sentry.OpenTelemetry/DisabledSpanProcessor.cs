using OpenTelemetry;

namespace Sentry.OpenTelemetry;

internal class DisabledSpanProcessor : BaseProcessor<Activity>
{
    private static readonly Lazy<DisabledSpanProcessor> LazyInstance = new();
    internal static DisabledSpanProcessor Instance => LazyInstance.Value;

    public override void OnStart(Activity activity)
    {
        // No-Op
    }

    public override void OnEnd(Activity activity)
    {
        // No-Op
    }
}

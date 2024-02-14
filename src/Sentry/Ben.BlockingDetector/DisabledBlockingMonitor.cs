namespace Sentry.Ben.BlockingDetector;

internal class DisabledBlockingMonitor : IBlockingMonitor
{
    private static readonly Lazy<DisabledBlockingMonitor> LazyInstance = new ();
    public static DisabledBlockingMonitor Instance => LazyInstance.Value;

    public void BlockingStart(DetectionSource detectionSource)
    {
        // No-op
    }

    public void BlockingEnd()
    {
        // No-op
    }
}

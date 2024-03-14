namespace Sentry.Ben.BlockingDetector;

internal class StaticTaskBlockingListenerState : ITaskBlockingListenerState
{
    [ThreadStatic] private static int SuppressionCount;

    public void Suppress() => SuppressionCount++;
    public bool IsSuppressed() => SuppressionCount > 0;

    public void Restore() => SuppressionCount--;
}

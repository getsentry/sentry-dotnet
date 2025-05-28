namespace Sentry.Ben.BlockingDetector;

internal interface ITaskBlockingListenerState
{
    public void Suppress();
    public bool IsSuppressed();
    public void Restore();
}

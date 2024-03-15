namespace Sentry.Ben.BlockingDetector;

internal interface ITaskBlockingListenerState
{
    void Suppress();
    bool IsSuppressed();
    void Restore();
}

namespace Sentry.Ben.BlockingDetector;

internal interface ITaskBlockingListenerState : IRecursionTracker
{
    void Suppress();
    bool IsSuppressed();
    void Restore();
}

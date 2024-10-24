namespace Sentry.Ben.BlockingDetector;

internal interface IBlockingMonitor
{
    void BlockingStart(DetectionSource detectionSource);
    void BlockingEnd();
}

internal enum DetectionSource
{
    SynchronizationContext,
    EventListener
}

namespace Sentry.Ben.BlockingDetector;

internal interface IBlockingMonitor
{
    public void BlockingStart(DetectionSource detectionSource);
    public void BlockingEnd();
}

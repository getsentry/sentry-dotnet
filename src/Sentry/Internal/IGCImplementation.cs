namespace Sentry.Internal;

/// <summary>
/// This allows us to test the GarbageCollectionMonitor class without a dependency on System.GC, which is static
/// </summary>
internal interface IGCImplementation
{
    public void RegisterForFullGCNotification(int maxGenerationThreshold, int largeObjectHeapThreshold);
    public GCNotificationStatus WaitForFullGCComplete(TimeSpan timeout);
    public void CancelFullGCNotification();
    public long TotalAvailableMemoryBytes { get; }
}

namespace Sentry.Internal;

/// <summary>
/// This allows us to test the GarbageCollectionMonitor class without a dependency on System.GC, which is static
/// </summary>
internal interface IGCImplementation
{
    void RegisterForFullGCNotification(int maxGenerationThreshold, int largeObjectHeapThreshold);
    GCNotificationStatus WaitForFullGCComplete(TimeSpan timeout);
    void CancelFullGCNotification();
    long TotalAvailableMemoryBytes { get; }
}

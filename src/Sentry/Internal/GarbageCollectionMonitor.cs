namespace Sentry.Internal;

/// <summary>
/// Simple class to determine when GarbageCollection occurs
/// </summary>
internal sealed class GarbageCollectionMonitor(Action onGarbageCollected)
{
    private const int MaxGenerationThreshold = 10;
    private const int LargeObjectHeapThreshold = 10;

    public void Start(CancellationToken cancellationToken) =>
        Task.Run(() => MonitorGarbageCollection(cancellationToken), cancellationToken);

    private void MonitorGarbageCollection(CancellationToken cancellationToken)
    {
        GC.RegisterForFullGCNotification(MaxGenerationThreshold, LargeObjectHeapThreshold);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (GC.WaitForFullGCComplete(1000) == GCNotificationStatus.Succeeded)
            {
                onGarbageCollected?.Invoke();
            }
        }

        GC.CancelFullGCNotification();
    }
}

namespace Sentry.Internal;

/// <summary>
/// Simple class to determine when GarbageCollection occurs
/// </summary>
internal sealed class GarbageCollectionMonitor(Action onGarbageCollected)
{
    private const int MaxGenerationThreshold = 1;
    private const int LargeObjectHeapThreshold = 1;

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

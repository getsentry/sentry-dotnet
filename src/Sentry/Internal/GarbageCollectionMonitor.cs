namespace Sentry.Internal;

/// <summary>
/// Simple class to detect when Full Garbage Collection occurs
/// </summary>
internal sealed class GarbageCollectionMonitor
{
    private const int MaxGenerationThreshold = 10;
    private const int LargeObjectHeapThreshold = 10;

    public static void Start(Action onGarbageCollected, CancellationToken cancellationToken) =>
        Task.Run(() => MonitorGarbageCollection(onGarbageCollected, cancellationToken), cancellationToken);

    private static void MonitorGarbageCollection(Action onGarbageCollected, CancellationToken cancellationToken)
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

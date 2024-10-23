using Sentry.Extensibility;

namespace Sentry.Internal;

/// <summary>
/// Simple class to detect when Full Garbage Collection occurs
/// </summary>
internal sealed class GarbageCollectionMonitor
{
    private const int MaxGenerationThreshold = 10;
    private const int LargeObjectHeapThreshold = 10;

    public static Task Start(Action onGarbageCollected, CancellationToken cancellationToken) =>
        Task.Run(() => MonitorGarbageCollection(onGarbageCollected, cancellationToken), cancellationToken);

    private static void MonitorGarbageCollection(Action onGarbageCollected, CancellationToken cancellationToken)
    {
        GC.RegisterForFullGCNotification(MaxGenerationThreshold, LargeObjectHeapThreshold);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
#if NET8_0_OR_GREATER
                if (GC.WaitForFullGCComplete(TimeSpan.FromSeconds(1)) == GCNotificationStatus.Succeeded)
#else
            if (GC.WaitForFullGCComplete(1000) == GCNotificationStatus.Succeeded)
#endif
                {
                    onGarbageCollected?.Invoke();
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Ignore
        }
        finally
        {
            GC.CancelFullGCNotification();
        }
    }
}

using Sentry.Extensibility;

namespace Sentry.Internal;

/// <summary>
/// Simple class to detect when Full Garbage Collection occurs
/// </summary>
internal sealed class GarbageCollectionMonitor
{
    private const int MaxGenerationThreshold = 10;
    private const int LargeObjectHeapThreshold = 10;

    public static Task Start(Action onGarbageCollected, CancellationToken cancellationToken, IGCImplementation? gc = null) =>
        Task.Run(() => MonitorGarbageCollection(onGarbageCollected, cancellationToken, gc), cancellationToken);

    private static void MonitorGarbageCollection(Action onGarbageCollected, CancellationToken cancellationToken, IGCImplementation? gc = null)
    {
        gc ??= new SystemGCImplementation();
        try
        {
            gc.RegisterForFullGCNotification(MaxGenerationThreshold, LargeObjectHeapThreshold);
            while (!cancellationToken.IsCancellationRequested)
            {
                if (gc.WaitForFullGCComplete(TimeSpan.FromSeconds(1)) == GCNotificationStatus.Succeeded)
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
            gc.CancelFullGCNotification();
        }
    }
}

using Sentry.Extensibility;

namespace Sentry.Internal;

/// <summary>
/// Simple class to detect when Full Garbage Collection occurs
/// </summary>
internal sealed class GarbageCollectionMonitor
{
    private const int MaxGenerationThreshold = 10;
    private const int LargeObjectHeapThreshold = 10;

    public static Task Start(SentryOptions options, Action onGarbageCollected, CancellationToken cancellationToken) =>
        Task.Run(() => MonitorGarbageCollection(options, onGarbageCollected, cancellationToken), cancellationToken);

    private static void MonitorGarbageCollection(SentryOptions options, Action onGarbageCollected, CancellationToken cancellationToken)
    {
        try
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
        catch (Exception e)
        {
            options.LogError("Error in GarbageCollectionMonitor: {0}", e);
        }
    }
}

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

    /// <summary>
    /// When not testing we use `System.GC`.
    /// </summary>
    /// <remarks>
    /// All these methods can throw an exception if concurrent garbage collection has been enabled in the runtime
    /// settings for the application.
    /// </remarks>
    private class SystemGCImplementation : IGCImplementation
    {
        public void RegisterForFullGCNotification(int maxGenerationThreshold, int largeObjectHeapThreshold) =>
            GC.RegisterForFullGCNotification(maxGenerationThreshold, largeObjectHeapThreshold);

        public GCNotificationStatus WaitForFullGCComplete(TimeSpan timeout) =>
#if NET8_0_OR_GREATER
            GC.WaitForFullGCComplete(timeout);
#else
        GC.WaitForFullGCComplete((int)timeout.TotalMilliseconds);
#endif

        public void CancelFullGCNotification() =>
            GC.CancelFullGCNotification();
    }
}

/// <summary>
/// This allows us to test the GarbageCollectionMonitor class without a dependency on System.GC, which is static
/// </summary>
internal interface IGCImplementation
{
    void RegisterForFullGCNotification(int maxGenerationThreshold, int largeObjectHeapThreshold);
    GCNotificationStatus WaitForFullGCComplete(TimeSpan timeout);
    void CancelFullGCNotification();
}

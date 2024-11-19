namespace Sentry.Internal;

/// <summary>
/// When not testing we use `System.GC`.
/// </summary>
/// <remarks>
/// All these methods can throw an exception if concurrent garbage collection has been enabled in the runtime
/// settings for the application.
/// </remarks>
internal class SystemGCImplementation : IGCImplementation
{
    public void RegisterForFullGCNotification(int maxGenerationThreshold, int largeObjectHeapThreshold) =>
        GC.RegisterForFullGCNotification(maxGenerationThreshold, largeObjectHeapThreshold);

    public GCNotificationStatus WaitForFullGCComplete(TimeSpan timeout) =>
#if NET8_0_OR_GREATER
        GC.WaitForFullGCComplete(timeout);
#else
    GC.WaitForFullGCComplete((int)timeout.TotalMilliseconds);
#endif

    public void CancelFullGCNotification() => GC.CancelFullGCNotification();

#if NET6_0_OR_GREATER
    public long TotalAvailableMemoryBytes => GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
#else
    public long TotalAvailableMemoryBytes => throw new PlatformNotSupportedException("This method is only available on .NET 5.0 or later.");
#endif
}

namespace Sentry.Samples.OpenTelemetry.Console;

internal class GarbageCollectionMonitor
{
    private static Action? OnGarbageCollected;

    ~GarbageCollectionMonitor()
    {
        if (Environment.HasShutdownStarted)
        {
            return;
        }

        if (AppDomain.CurrentDomain.IsFinalizingForUnload())
        {
            return;
        }

        CreateDanglingMonitor();
        OnGarbageCollected?.Invoke();
    }

    private static void CreateDanglingMonitor()
    {
        // ReSharper disable once ObjectCreationAsStatement
#pragma warning disable CA1806
        new GarbageCollectionMonitor();
#pragma warning restore CA1806
    }

    public static void Start(Action onGarbageCollected)
    {
        OnGarbageCollected += onGarbageCollected;
        CreateDanglingMonitor();
    }
}

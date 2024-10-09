namespace Sentry.Internal;

/// <summary>
/// Simple class to determine when GarbageCollection occurs
/// </summary>
internal sealed class GarbageCollectionMonitor
{
    private readonly CancellationToken _cancellationToken;
    private readonly Action _onGarbageCollected;

    private GarbageCollectionMonitor(Action onGarbageCollected, CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
        _onGarbageCollected = onGarbageCollected;
    }

    ~GarbageCollectionMonitor()
    {
        // If monitoring has stopped or the App is shutting down, stop monitoring.
        var stopped = _cancellationToken.IsCancellationRequested;
        if (stopped || Environment.HasShutdownStarted || AppDomain.CurrentDomain.IsFinalizingForUnload())
        {
            return;
        }

        // Every time this class gets cleaned up by the GC, we create another dangling instance of the class (which will
        // be cleaned up in the next GC cycle)... so we keep creating dangling references as long as we want to keep
        // monitoring.
        CreateDanglingMonitor(_onGarbageCollected, _cancellationToken);
        _onGarbageCollected?.Invoke();
    }

    private static void CreateDanglingMonitor(Action onGarbageCollected, CancellationToken cancellationToken)
    {
        // ReSharper disable once ObjectCreationAsStatement
#pragma warning disable CA1806
        new GarbageCollectionMonitor(onGarbageCollected, cancellationToken);
#pragma warning restore CA1806
    }

    public static void Start(Action onGarbageCollected, CancellationToken cancellationToken = default) =>
        CreateDanglingMonitor(onGarbageCollected, cancellationToken);
}

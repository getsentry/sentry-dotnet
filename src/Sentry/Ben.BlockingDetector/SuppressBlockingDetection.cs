namespace Sentry.Ben.BlockingDetector;

/// <summary>
/// Controls blocking detection suppression
/// </summary>
public class SuppressBlockingDetection : IDisposable
{
    internal readonly ITaskBlockingListenerState _listener;
    internal readonly DetectBlockingSynchronizationContext? _context;

    /// <summary>
    /// Suppresses blocking detection for a particular code block
    /// </summary>
    /// <example>
    /// using (new SuppressBlockingDetection())
    /// {
    ///     Task.Delay(10).Wait(); // Will not trigger a blocking detection event due to suppression
    /// }
    /// </example>
    public SuppressBlockingDetection()
        : this(SynchronizationContext.Current as DetectBlockingSynchronizationContext, TaskBlockingListener.DefaultState)
    {
    }

    internal SuppressBlockingDetection(DetectBlockingSynchronizationContext? context, ITaskBlockingListenerState listener)
    {
        _context = context;
        _listener = listener;

        _context?.Suppress();
        _listener.Suppress();
    }

    /// <summary>
    /// Reinstates previous blocking detection behavior
    /// </summary>
    public void Dispose()
    {
        _listener.Restore();
        _context?.Restore();
    }
}

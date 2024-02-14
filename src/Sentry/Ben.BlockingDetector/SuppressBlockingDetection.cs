using Sentry.Internal;

namespace Sentry.Ben.BlockingDetector;

/// <summary>
/// Controls blocking detection suppression
/// </summary>
public class SuppressBlockingDetection : IDisposable
{
    private readonly DetectBlockingSynchronizationContext? _context;

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
    {
        _context = SynchronizationContext.Current as DetectBlockingSynchronizationContext;
        _context?.Suppress();

        TaskBlockingListener.Suppress();
    }

    /// <summary>
    /// Reinstates previous blocking detection behavior
    /// </summary>
    public void Dispose()
    {
        TaskBlockingListener.Restore();

        _context?.Restore();
    }
}

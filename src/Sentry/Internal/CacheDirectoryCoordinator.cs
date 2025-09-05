using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internal;

internal class CacheDirectoryCoordinator : IDisposable
{
    private readonly IDiagnosticLogger? _logger;
    private readonly Semaphore _semaphore;
    private bool _acquired;
    private bool _disposed;
    private readonly Lock _gate = new();

    public CacheDirectoryCoordinator(string cacheDir, IDiagnosticLogger? logger)
    {
        _logger = logger;
        var mutexName = BuildMutexName(cacheDir);
        _semaphore = new Semaphore(1, 1, mutexName); // Named mutexes allow interprocess locks on all platforms
    }

    private static string BuildMutexName(string path)
    {
        var hash = path.GetHashString();
        // Global\ prefix allows cross-session visibility on Windows Terminal Services
        return $"Global\\SentryCache{hash}";
    }

    /// <summary>
    /// Try to own this cache directory.
    /// </summary>
    /// <param name="timeout">How long to wait for the lock.</param>
    /// <returns>True if acquired; false otherwise.</returns>
    public bool TryAcquire(TimeSpan timeout)
    {
        if (_acquired)
        {
            return true;
        }
        lock (_gate)
        {
            if (_acquired)
            {
                return true;
            }
            if (!_semaphore.WaitOne(timeout))
            {
                return false;
            }
            _acquired = true;
            return true;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_acquired)
            {
                try { _semaphore.Release(); }
                catch (Exception ex) { _logger?.LogError("Error releasing the cache directory semaphore.", ex); }
            }
            _semaphore.Dispose();
        }
    }
}

internal static class CacheDirectoryHelper
{
    public const string IsolatedCacheDirectoryPrefix = "isolated_";

    internal static string? GetBaseCacheDirectoryPath(this SentryOptions options) =>
        string.IsNullOrWhiteSpace(options.CacheDirectoryPath)
            ? null
            : Path.Combine(options.CacheDirectoryPath, "Sentry");

    internal static string? TryGetIsolatedCacheDirectoryPath(this SentryOptions options)
    {
        if (GetBaseCacheDirectoryPath(options) is not {} baseCacheDir || string.IsNullOrWhiteSpace(options.Dsn))
        {
            return null;
        }

        var stringBuilder = new StringBuilder(IsolatedCacheDirectoryPrefix);
#if IOS || ANDROID
        // On iOS or Android the app is already sandboxed, so there's no risk of sending data to another Sentry's DSN.
        // However, users may still initiate the SDK multiple times within the process, so we need an InitCounter
        stringBuilder.Append(options.InitCounter.Count);
#else
        var processId = options.ProcessIdResolver.Invoke() ?? 0;
        stringBuilder.AppendJoin('_', options.Dsn.GetHashString(), processId, options.InitCounter.Count);
#endif
        return Path.Combine(baseCacheDir, stringBuilder.ToString());
    }
}

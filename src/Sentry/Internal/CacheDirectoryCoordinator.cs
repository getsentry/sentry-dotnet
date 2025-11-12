using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internal;

internal class CacheDirectoryCoordinator : IDisposable
{
    private readonly IDiagnosticLogger? _logger;
    private readonly IFileSystem _fileSystem;
    private readonly Lock _lock = new();

    private Stream? _lockStream;
    private readonly string _lockFilePath;

    private volatile bool _acquired;
    private volatile bool _disposed;

    public CacheDirectoryCoordinator(string cacheDir, IDiagnosticLogger? logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        // Note this creates a lock file in the cache directory's parent directory... not in the cache directory itself
        _lockFilePath = $"{cacheDir}.lock";
    }

    public bool TryAcquire()
    {
        if (_acquired)
        {
            return true;
        }

        lock (_lock)
        {
            if (_acquired)
            {
                return true;
            }

            if (_disposed)
            {
                return false;
            }

            try
            {
                var baseDir = Path.GetDirectoryName(_lockFilePath);
                if (!string.IsNullOrWhiteSpace(baseDir))
                {
                    _fileSystem.CreateDirectory(baseDir);
                }
                _acquired = _fileSystem.TryCreateLockFile(_lockFilePath, out _lockStream);
                return _acquired;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug("Unable to acquire cache directory lock", ex);
            }
            finally
            {
                if (!_acquired && _lockStream is not null)
                {
                    try
                    { _lockStream.Dispose(); }
                    catch
                    {
                        // Ignore
                    }
                    _lockStream = null;
                }
            }

            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        lock (_lock)
        {
            _disposed = true;

            if (_acquired)
            {
                try
                {
                    _lockStream?.Close();
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Error releasing the cache directory file lock.", ex);
                }
            }

            try
            {
                _lockStream?.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error disposing cache lock stream.", ex);
            }
            _lockStream = null;
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

    internal static string? GetIsolatedFolderName(this SentryOptions options)
    {
        var stringBuilder = new StringBuilder(IsolatedCacheDirectoryPrefix);
#if IOS || ANDROID
        // On iOS or Android the app is already sandboxed, so there's no risk of sending data to another Sentry's DSN.
        // However, users may still initiate the SDK multiple times within the process, so we need an InitCounter
        stringBuilder.Append(options.InitCounter.Count);
#else
        if (string.IsNullOrWhiteSpace(options.Dsn))
        {
            return null;
        }
        var processId = options.ProcessIdResolver.Invoke() ?? 0;
        stringBuilder.AppendJoin('_', options.Dsn.GetHashString(), processId.ToString(),
            options.InitCounter.Count.ToString());
#endif
        return stringBuilder.ToString();
    }

    internal static string? GetIsolatedCacheDirectoryPath(this SentryOptions options) =>
        GetBaseCacheDirectoryPath(options) is not { } baseCacheDir
        || GetIsolatedFolderName(options) is not { } isolatedFolderName
            ? null
            : Path.Combine(baseCacheDir, isolatedFolderName);
}

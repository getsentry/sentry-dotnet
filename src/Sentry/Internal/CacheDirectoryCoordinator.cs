using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internal;

internal class CacheDirectoryCoordinator : IDisposable
{
    private readonly IDiagnosticLogger? _logger;
    private readonly IFileSystem _fileSystem;
    private readonly object _gate = new();

    private Stream? _lockStream;
    private readonly string _lockFilePath;

    private bool _acquired;
    private bool _disposed;

    public CacheDirectoryCoordinator(string cacheDir, IDiagnosticLogger? logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _lockFilePath = $"{cacheDir}.lock";

        try
        {
            var baseDir = Path.GetDirectoryName(_lockFilePath);
            if (!string.IsNullOrWhiteSpace(baseDir))
            {
                // Not normally necessary, but just in case
                fileSystem.CreateDirectory(baseDir);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Failed to ensure lock directory exists for cache coordinator.", ex);
        }
    }

    public bool TryAcquire()
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

            if (_disposed)
            {
                return false;
            }

            try
            {
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
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

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

    private static string? GetIsolatedFolderName(this SentryOptions options)
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
        stringBuilder.AppendJoin('_', options.Dsn.GetHashString(), processId, options.InitCounter.Count);
#endif
        return stringBuilder.ToString();
    }

    internal static string? TryGetIsolatedCacheDirectoryPath(this SentryOptions options) =>
        GetBaseCacheDirectoryPath(options) is not { } baseCacheDir
        || GetIsolatedFolderName(options) is not { } isolatedFolderName
            ? null
            : Path.Combine(baseCacheDir, isolatedFolderName);
}

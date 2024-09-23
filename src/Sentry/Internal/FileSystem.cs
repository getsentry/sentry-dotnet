using Sentry.Extensibility;

namespace Sentry.Internal;

internal class FileSystem : IFileSystem
{
    private readonly SentryOptions? _options;

    public FileSystem(SentryOptions? options)
    {
        _options = options;
    }

    public IEnumerable<string> EnumerateFiles(string path) => Directory.EnumerateFiles(path);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern) =>
        Directory.EnumerateFiles(path, searchPattern);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) =>
        Directory.EnumerateFiles(path, searchPattern, searchOption);

    public DirectoryInfo? CreateDirectory(string path)
    {
        if (!_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping creating directory. Writing to file system has been explicitly disabled.");
            return null;
        }

        return Directory.CreateDirectory(path);
    }

    public bool? DeleteDirectory(string path, bool recursive = false)
    {
        if (!_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping deleting directory. Writing to file system has been explicitly disabled.");
            return false;
        }

        Directory.Delete(path, recursive);
        return !Directory.Exists(path);
    }

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public bool? MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
    {
        if (!_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping moving file. Writing to file system has been explicitly disabled.");
            return null;
        }

#if NETCOREAPP3_0_OR_GREATER
        File.Move(sourceFileName, destFileName, overwrite);
#else
        if (overwrite)
        {
            File.Copy(sourceFileName, destFileName, overwrite: true);
            File.Delete(sourceFileName);
        }
        else
        {
            File.Move(sourceFileName, destFileName);
        }
#endif

        if (File.Exists(sourceFileName) || !File.Exists(destFileName))
        {
            return false;
        }

        return true;
    }

    public bool? DeleteFile(string path)
    {
        if (!_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping deleting file. Writing to file system has been explicitly disabled.");
            return null;
        }

        File.Delete(path);
        return !File.Exists(path);
    }

    public DateTimeOffset GetFileCreationTime(string path) => new FileInfo(path).CreationTimeUtc;

    public string ReadAllTextFromFile(string path) => File.ReadAllText(path);

    public Stream OpenFileForReading(string path) => File.OpenRead(path);

    public Stream OpenFileForReading(string path, bool useAsync, FileMode fileMode = FileMode.Open, FileAccess fileAccess = FileAccess.Read, FileShare fileShare = FileShare.ReadWrite, int bufferSize = 4096)
    {
        return new FileStream(
            path,
            fileMode,
            fileAccess,
            fileShare,
            bufferSize: bufferSize,
            useAsync: useAsync);
    }

    public Stream? CreateFileForWriting(string path)
    {
        if (!_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping file for writing. Writing to file system has been explicitly disabled.");
            return null;
        }

        return File.Create(path);
    }

    public bool? WriteAllTextToFile(string path, string contents)
    {
        if (!_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping writing all text to file. Writing to file system has been explicitly disabled.");
            return null;
        }

        File.WriteAllText(path, contents);
        return File.Exists(path);
    }
}

using Sentry.Extensibility;

namespace Sentry.Internal;

internal class FileSystem : IFileSystem
{
    public static IFileSystem Instance { get; } = new FileSystem(SentrySdk.CurrentOptions);

    private readonly SentryOptions? _options;

    private FileSystem(SentryOptions? options)
    {
        _options = options;
    }

    public IEnumerable<string> EnumerateFiles(string path) => Directory.EnumerateFiles(path);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern) =>
        Directory.EnumerateFiles(path, searchPattern);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) =>
        Directory.EnumerateFiles(path, searchPattern, searchOption);

    public bool CreateDirectory(string path)
    {
        if (_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping creating directory. Writing to file system has been explicitly disabled.");
            return false;
        }

        Directory.CreateDirectory(path);
        return true;
    }

    public bool DeleteDirectory(string path, bool recursive = false)
    {
        if (_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping deleting directory. Writing to file system has been explicitly disabled.");
            return false;
        }

        Directory.Delete(path, recursive);
        return true;
    }

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public bool MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
    {
        if (_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping moving file. Writing to file system has been explicitly disabled.");
            return false;
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
        return true;
    }

    public bool DeleteFile(string path)
    {
        if (_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping deleting file. Writing to file system has been explicitly disabled.");
            return false;
        }

        File.Delete(path);
        return true;
    }

    public DateTimeOffset GetFileCreationTime(string path) => new FileInfo(path).CreationTimeUtc;

    public string ReadAllTextFromFile(string path) => File.ReadAllText(path);

    public Stream OpenFileForReading(string path) => File.OpenRead(path);

    public Stream CreateFileForWriting(string path)
    {
        if (_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping file for writing. Writing to file system has been explicitly disabled.");
            return Stream.Null;
        }

        return File.Create(path);
    }

    public bool WriteAllTextToFile(string path, string contents)
    {
        if (_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping writing all text to file. Writing to file system has been explicitly disabled.");
            return false;
        }

        File.WriteAllText(path, contents);
        return true;
    }
}

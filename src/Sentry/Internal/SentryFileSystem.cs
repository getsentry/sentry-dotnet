using System.IO.Abstractions;
using Sentry.Extensibility;

namespace Sentry.Internal;

internal class SentryFileSystem : ISentryFileSystem
{
    private readonly SentryOptions? _options;

    private readonly IFileSystem _fileSystem;

    public SentryFileSystem(SentryOptions? options, IFileSystem? fileSystem = null)
    {
        _options = options;
        _fileSystem = fileSystem ?? new FileSystem();
    }

    public IEnumerable<string> EnumerateFiles(string path) => _fileSystem.Directory.EnumerateFiles(path);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern) =>
        _fileSystem.Directory.EnumerateFiles(path, searchPattern);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) =>
        _fileSystem.Directory.EnumerateFiles(path, searchPattern, searchOption);

    public bool CreateDirectory(string path)
    {
        if (!_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping creating directory. Writing to file system has been explicitly disabled.");
            return false;
        }

        _fileSystem.Directory.CreateDirectory(path);
        return true;
    }

    public bool DeleteDirectory(string path, bool recursive = false)
    {
        if (!_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping deleting directory. Writing to file system has been explicitly disabled.");
            return false;
        }

        _fileSystem.Directory.Delete(path, recursive);
        return true;
    }

    public bool DirectoryExists(string path) => _fileSystem.Directory.Exists(path);

    public bool FileExists(string path) => _fileSystem.File.Exists(path);

    public bool MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
    {
        if (!_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping moving file. Writing to file system has been explicitly disabled.");
            return false;
        }

#if NETCOREAPP3_0_OR_GREATER
        _fileSystem.File.Move(sourceFileName, destFileName, overwrite);
#else
        if (overwrite)
        {
            _fileSystem.File.Copy(sourceFileName, destFileName, overwrite: true);
            _fileSystem.File.Delete(sourceFileName);
        }
        else
        {
            _fileSystem.File.Move(sourceFileName, destFileName);
        }
#endif
        return true;
    }

    public bool DeleteFile(string path)
    {
        if (!_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping deleting file. Writing to file system has been explicitly disabled.");
            return false;
        }

        _fileSystem.File.Delete(path);
        return true;
    }

    public DateTimeOffset GetFileCreationTime(string path) => new FileInfo(path).CreationTimeUtc;

    public string ReadAllTextFromFile(string path) => _fileSystem.File.ReadAllText(path);

    public Stream OpenFileForReading(string path) => _fileSystem.File.OpenRead(path);

    public Stream CreateFileForWriting(string path)
    {
        if (!_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping file for writing. Writing to file system has been explicitly disabled.");
            return Stream.Null;
        }

        return _fileSystem.File.Create(path);
    }

    public bool WriteAllTextToFile(string path, string contents)
    {
        if (!_options?.DisableFileWrite is false)
        {
            _options?.LogDebug("Skipping writing all text to file. Writing to file system has been explicitly disabled.");
            return false;
        }

        _fileSystem.File.WriteAllText(path, contents);
        return true;
    }
}

using System.IO.Abstractions.TestingHelpers;

namespace Sentry.Testing;

internal class FakeFileSystem : IFileSystem
{
    private readonly MockFileSystem _fileSystem = new();

    public IEnumerable<string> EnumerateFiles(string path) => _fileSystem.Directory.EnumerateFiles(path);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern) =>
        _fileSystem.Directory.EnumerateFiles(path, searchPattern);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) =>
        _fileSystem.Directory.EnumerateFiles(path, searchPattern, searchOption);

    public bool DirectoryExists(string path) => _fileSystem.Directory.Exists(path);

    public bool FileExists(string path) => _fileSystem.File.Exists(path);

    public DateTimeOffset GetFileCreationTime(string path) => new FileInfo(path).CreationTimeUtc;

    public string ReadAllTextFromFile(string path) => _fileSystem.File.ReadAllText(path);

    public Stream OpenFileForReading(string path) => _fileSystem.File.OpenRead(path);

    public bool CreateDirectory(string path)
    {
        _fileSystem.Directory.CreateDirectory(path);
        return _fileSystem.Directory.Exists(path);
    }

    public bool DeleteDirectory(string path, bool recursive = false)
    {
        _fileSystem.Directory.Delete(path, recursive);
        return !_fileSystem.Directory.Exists(path);
    }

    public bool CreateFileForWriting(string path, out Stream stream)
    {
        stream = _fileSystem.File.Create(path);
        return true;
    }

    public bool WriteAllTextToFile(string path, string contents)
    {
        _fileSystem.File.WriteAllText(path, contents);
        return _fileSystem.File.Exists(path);
    }

    public bool MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
    {
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

        if (_fileSystem.File.Exists(sourceFileName) || !_fileSystem.File.Exists(destFileName))
        {
            return false;
        }

        return true;
    }

    public bool DeleteFile(string path)
    {
        _fileSystem.File.Delete(path);
        return !_fileSystem.File.Exists(path);
    }
}

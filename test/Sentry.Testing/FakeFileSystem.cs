using System.IO.Abstractions.TestingHelpers;

namespace Sentry.Internal;

internal class FakeFileSystem : IFileSystem
{
    private readonly MockFileSystem _fileSystem = new();

    public IEnumerable<string> EnumerateFiles(string path) => _fileSystem.Directory.EnumerateFiles(path);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern) =>
        _fileSystem.Directory.EnumerateFiles(path, searchPattern);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) =>
        _fileSystem.Directory.EnumerateFiles(path, searchPattern, searchOption);

    public FileOperationResult CreateDirectory(string path)
    {
        _fileSystem.Directory.CreateDirectory(path);
        return _fileSystem.Directory.Exists(path) ? FileOperationResult.Success : FileOperationResult.Failure;
    }

    public FileOperationResult DeleteDirectory(string path, bool recursive = false)
    {
        _fileSystem.Directory.Delete(path, recursive);
        return _fileSystem.Directory.Exists(path) ? FileOperationResult.Failure : FileOperationResult.Success;
    }

    public bool DirectoryExists(string path) => _fileSystem.Directory.Exists(path);

    public bool FileExists(string path) => _fileSystem.File.Exists(path);

    public FileOperationResult MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
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
            return FileOperationResult.Failure;
        }

        return FileOperationResult.Success;
    }

    public FileOperationResult DeleteFile(string path)
    {
        _fileSystem.File.Delete(path);
        return _fileSystem.File.Exists(path) ? FileOperationResult.Failure : FileOperationResult.Success;
    }

    public DateTimeOffset GetFileCreationTime(string path) => new FileInfo(path).CreationTimeUtc;

    public string ReadAllTextFromFile(string path) => _fileSystem.File.ReadAllText(path);

    public Stream OpenFileForReading(string path) => _fileSystem.File.OpenRead(path);

    public Stream OpenFileForReading(string path,
        bool useAsync,
        FileMode fileMode = FileMode.Open,
        FileAccess fileAccess = FileAccess.Read,
        FileShare fileShare = FileShare.ReadWrite,
        int bufferSize = 4096)
    {
        return new FileStream(
            path,
            fileMode,
            fileAccess,
            fileShare,
            bufferSize: bufferSize,
            useAsync: useAsync);
    }

    public FileOperationResult CreateFileForWriting(string path, out Stream stream)
    {
        stream = _fileSystem.File.Create(path);
        return FileOperationResult.Success;
    }

    public FileOperationResult WriteAllTextToFile(string path, string contents)
    {
        _fileSystem.File.WriteAllText(path, contents);
        return _fileSystem.File.Exists(path) ? FileOperationResult.Success : FileOperationResult.Failure;
    }
}

using Sentry.Extensibility;

namespace Sentry.Internal;

internal class ReadOnlyFileSystem : IFileSystem
{
    public IEnumerable<string> EnumerateFiles(string path) => Directory.EnumerateFiles(path);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern) =>
        Directory.EnumerateFiles(path, searchPattern);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) =>
        Directory.EnumerateFiles(path, searchPattern, searchOption);

    public FileOperationResult CreateDirectory(string path) => FileOperationResult.Disabled;

    public FileOperationResult DeleteDirectory(string path, bool recursive = false) => FileOperationResult.Disabled;

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public FileOperationResult MoveFile(string sourceFileName, string destFileName, bool overwrite = false) =>
        FileOperationResult.Disabled;

    public FileOperationResult DeleteFile(string path) => FileOperationResult.Disabled;

    public DateTimeOffset GetFileCreationTime(string path) => new FileInfo(path).CreationTimeUtc;

    public string ReadAllTextFromFile(string path) => File.ReadAllText(path);

    public Stream OpenFileForReading(string path) => File.OpenRead(path);

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

    public (FileOperationResult, Stream) CreateFileForWriting(string path) => (FileOperationResult.Disabled, Stream.Null);

    public FileOperationResult WriteAllTextToFile(string path, string contents)
    {
        File.WriteAllText(path, contents);
        return File.Exists(path) ? FileOperationResult.Success : FileOperationResult.Failure;
    }
}

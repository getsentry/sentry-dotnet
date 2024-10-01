namespace Sentry.Internal;

internal abstract class FileSystemBase : IFileSystem
{
    public IEnumerable<string> EnumerateFiles(string path) => Directory.EnumerateFiles(path);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern) =>
        Directory.EnumerateFiles(path, searchPattern);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) =>
        Directory.EnumerateFiles(path, searchPattern, searchOption);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public DateTimeOffset GetFileCreationTime(string path) => new FileInfo(path).CreationTimeUtc;

    public string ReadAllTextFromFile(string path) => File.ReadAllText(path);

    public Stream OpenFileForReading(string path) => File.OpenRead(path);

    public abstract FileOperationResult CreateDirectory(string path);
    public abstract FileOperationResult DeleteDirectory(string path, bool recursive = false);
    public abstract FileOperationResult CreateFileForWriting(string path, out Stream fileStream);
    public abstract FileOperationResult WriteAllTextToFile(string path, string contents);
    public abstract FileOperationResult MoveFile(string sourceFileName, string destFileName, bool overwrite = false);
    public abstract FileOperationResult DeleteFile(string path);
}

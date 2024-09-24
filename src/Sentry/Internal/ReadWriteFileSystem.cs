namespace Sentry.Internal;

internal class ReadWriteFileSystem : IFileSystem
{
    public IEnumerable<string> EnumerateFiles(string path) => Directory.EnumerateFiles(path);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern) =>
        Directory.EnumerateFiles(path, searchPattern);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) =>
        Directory.EnumerateFiles(path, searchPattern, searchOption);

    public FileOperationResult CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return DirectoryExists(path) ? FileOperationResult.Success : FileOperationResult.Failure;
    }

    public FileOperationResult DeleteDirectory(string path, bool recursive = false)
    {
        Directory.Delete(path, recursive);
        return Directory.Exists(path)  ? FileOperationResult.Failure : FileOperationResult.Success;
    }

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public FileOperationResult MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
    {
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
            return FileOperationResult.Failure;
        }

        return FileOperationResult.Success;
    }

    public FileOperationResult DeleteFile(string path)
    {
        File.Delete(path);
        return File.Exists(path) ? FileOperationResult.Failure : FileOperationResult.Success;
    }

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

    public (FileOperationResult, Stream) CreateFileForWriting(string path)
    {
        return (FileOperationResult.Success, File.Create(path));
    }

    public FileOperationResult WriteAllTextToFile(string path, string contents)
    {
        File.WriteAllText(path, contents);
        return File.Exists(path) ? FileOperationResult.Success : FileOperationResult.Failure;
    }
}

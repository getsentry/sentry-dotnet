namespace Sentry.Internal;

internal enum FileOperationResult
{
    Success,
    Failure,
    Disabled
}

internal interface IFileSystem
{
    // Note: This is not comprehensive.  If you need other filesystem methods, add to this interface,
    // then implement in both Sentry.Internal.FileSystem and Sentry.Testing.FakeFileSystem.

    IEnumerable<string> EnumerateFiles(string path);
    IEnumerable<string> EnumerateFiles(string path, string searchPattern);
    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);
    FileOperationResult CreateDirectory(string path);
    FileOperationResult DeleteDirectory(string path, bool recursive = false);
    bool DirectoryExists(string path);
    bool FileExists(string path);
    FileOperationResult MoveFile(string sourceFileName, string destFileName, bool overwrite = false);
    FileOperationResult DeleteFile(string path);
    DateTimeOffset GetFileCreationTime(string path);
    string? ReadAllTextFromFile(string file);
    Stream OpenFileForReading(string path);
    Stream OpenFileForReading(string path,
        bool useAsync,
        FileMode fileMode = FileMode.Open,
        FileAccess fileAccess = FileAccess.Read,
        FileShare fileShare = FileShare.ReadWrite,
        int bufferSize = 4096);
    (FileOperationResult, Stream) CreateFileForWriting(string path);
    FileOperationResult WriteAllTextToFile(string path, string contents);
}

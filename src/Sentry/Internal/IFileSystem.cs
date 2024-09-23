namespace Sentry.Internal;

internal interface IFileSystem
{
    // Note: This is not comprehensive.  If you need other filesystem methods, add to this interface,
    // then implement in both Sentry.Internal.FileSystem and Sentry.Testing.FakeFileSystem.

    IEnumerable<string> EnumerateFiles(string path);
    IEnumerable<string> EnumerateFiles(string path, string searchPattern);
    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);
    DirectoryInfo? CreateDirectory(string path);
    bool? DeleteDirectory(string path, bool recursive = false);
    bool DirectoryExists(string path);
    bool FileExists(string path);
    bool? MoveFile(string sourceFileName, string destFileName, bool overwrite = false);
    bool? DeleteFile(string path);
    DateTimeOffset GetFileCreationTime(string path);
    string? ReadAllTextFromFile(string file);
    Stream? OpenFileForReading(string path);
    Stream? OpenFileForReading(string path,
        bool useAsync,
        FileMode fileMode = FileMode.Open,
        FileAccess fileAccess = FileAccess.Read,
        FileShare fileShare = FileShare.ReadWrite,
        int bufferSize = 4096);
    Stream? CreateFileForWriting(string path);
    bool? WriteAllTextToFile(string path, string contents);
}

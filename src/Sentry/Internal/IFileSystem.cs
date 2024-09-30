namespace Sentry.Internal;

internal interface IFileSystem
{
    // Note: This is not comprehensive.  If you need other filesystem methods, add to this interface,
    // then implement in both Sentry.Internal.FileSystem and Sentry.Testing.FakeFileSystem.

    IEnumerable<string> EnumerateFiles(string path);
    IEnumerable<string> EnumerateFiles(string path, string searchPattern);
    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);
    bool DirectoryExists(string path);
    bool FileExists(string path);
    DateTimeOffset GetFileCreationTime(string path);
    string? ReadAllTextFromFile(string file);
    Stream OpenFileForReading(string path);

    bool CreateDirectory(string path);
    bool DeleteDirectory(string path, bool recursive = false);
    bool CreateFileForWriting(string path, out Stream fileStream);
    bool WriteAllTextToFile(string path, string contents);
    bool MoveFile(string sourceFileName, string destFileName, bool overwrite = false);
    bool DeleteFile(string path);
}

namespace Sentry.Internal;

internal interface IFileSystem
{
    // Note: You are responsible for handling success/failure when attempting to write to disk.
    // You are required to check for `Options.FileWriteDisabled` whether you are allowed to call any writing operations.
    // The options will automatically pick between `ReadOnly` and `ReadAndWrite` to prevent accidental file writing that
    // could cause crashes on restricted platforms like the Nintendo Switch.

    // Note: This is not comprehensive.  If you need other filesystem methods, add to this interface,
    // then implement in both Sentry.Internal.FileSystem and Sentry.Testing.FakeFileSystem.

    public IEnumerable<string> EnumerateFiles(string path);
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern);
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);
    public bool DirectoryExists(string path);
    public bool FileExists(string path);
    public DateTimeOffset GetFileCreationTime(string path);
    public string? ReadAllTextFromFile(string file);
    public Stream OpenFileForReading(string path);

    public bool CreateDirectory(string path);
    public bool DeleteDirectory(string path, bool recursive = false);
    public bool CreateFileForWriting(string path, out Stream fileStream);
    public bool WriteAllTextToFile(string path, string contents);
    public bool MoveFile(string sourceFileName, string destFileName, bool overwrite = false);
    public bool DeleteFile(string path);
}

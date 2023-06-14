namespace Sentry.Internal;

internal class FileSystem : IFileSystem
{
    public static IFileSystem Instance { get; } = new FileSystem();

    private FileSystem()
    {
    }

    public IEnumerable<string> EnumerateFiles(string path) => Directory.EnumerateFiles(path);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern) =>
        Directory.EnumerateFiles(path, searchPattern);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) =>
        Directory.EnumerateFiles(path, searchPattern, searchOption);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void DeleteDirectory(string path, bool recursive = false) => Directory.Delete(path, recursive);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public void MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
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
    }

    public void DeleteFile(string path) => File.Delete(path);

    public DateTimeOffset GetFileCreationTime(string path) => new FileInfo(path).CreationTimeUtc;

    public string ReadAllTextFromFile(string path) => File.ReadAllText(path);

    public Stream OpenFileForReading(string path) => File.OpenRead(path);

    public Stream CreateFileForWriting(string path) => File.Create(path);

    public Stream GetLeaseFile(string path) => File.Open(path, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
}

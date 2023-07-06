using MockFileSystem = System.IO.Abstractions.TestingHelpers.MockFileSystem;

namespace Sentry.Testing;

public class FakeFileSystem : IFileSystem
{
    // This is an in-memory implementation provided by https://github.com/TestableIO/System.IO.Abstractions
    private readonly MockFileSystem _mockFileSystem = new();

    public IEnumerable<string> EnumerateFiles(string path) => _mockFileSystem.Directory.EnumerateFiles(path);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern) =>
        _mockFileSystem.Directory.EnumerateFiles(path, searchPattern);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) =>
        _mockFileSystem.Directory.EnumerateFiles(path, searchPattern, searchOption);

    public void CreateDirectory(string path) => _mockFileSystem.Directory.CreateDirectory(path);

    public void DeleteDirectory(string path, bool recursive = false) =>
        _mockFileSystem.Directory.Delete(path, recursive);

    public bool DirectoryExists(string path) => _mockFileSystem.Directory.Exists(path);

    public bool FileExists(string path) => _mockFileSystem.File.Exists(path);

    public void MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
    {
#if NET5_0_OR_GREATER
        _mockFileSystem.File.Move(sourceFileName, destFileName, overwrite);
#else
        if (overwrite)
        {
            _mockFileSystem.File.Copy(sourceFileName, destFileName, overwrite: true);
            _mockFileSystem.File.Delete(sourceFileName);
        }
        else
        {
            _mockFileSystem.File.Move(sourceFileName, destFileName);
        }
#endif
    }

    public void DeleteFile(string path) => _mockFileSystem.File.Delete(path);

    public DateTimeOffset GetFileCreationTime(string path) =>
        _mockFileSystem.FileInfo.New(path).CreationTimeUtc;

    public string ReadAllTextFromFile(string file) => _mockFileSystem.File.ReadAllText(file);

    public Stream OpenFileForReading(string path) => _mockFileSystem.File.OpenRead(path);

    public Stream CreateFileForWriting(string path) => _mockFileSystem.File.Create(path);
}

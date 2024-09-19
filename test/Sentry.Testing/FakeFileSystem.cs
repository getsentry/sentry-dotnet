using MockFileSystem = System.IO.Abstractions.TestingHelpers.MockFileSystem;

namespace Sentry.Testing;

public class FakeFileSystem : IFileSystem
{
    // This is an in-memory implementation provided by https://github.com/TestableIO/System.IO.Abstractions
    public readonly MockFileSystem MockFileSystem = new();

    public IEnumerable<string> EnumerateFiles(string path) => MockFileSystem.Directory.EnumerateFiles(path);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern) =>
        MockFileSystem.Directory.EnumerateFiles(path, searchPattern);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) =>
        MockFileSystem.Directory.EnumerateFiles(path, searchPattern, searchOption);

    public bool CreateDirectory(string path)
    {
        MockFileSystem.Directory.CreateDirectory(path);
        return true;
    }

    public bool DeleteDirectory(string path, bool recursive = false)
    {
        MockFileSystem.Directory.Delete(path, recursive);
        return true;
    }

    public bool DirectoryExists(string path) => MockFileSystem.Directory.Exists(path);

    public bool FileExists(string path) => MockFileSystem.File.Exists(path);

    public bool MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
    {
#if NET5_0_OR_GREATER
        MockFileSystem.File.Move(sourceFileName, destFileName, overwrite);
#else
        if (overwrite)
        {
            MockFileSystem.File.Copy(sourceFileName, destFileName, overwrite: true);
            MockFileSystem.File.Delete(sourceFileName);
        }
        else
        {
            MockFileSystem.File.Move(sourceFileName, destFileName);
        }
#endif

        return true;
    }

    public bool DeleteFile(string path)
    {
        MockFileSystem.File.Delete(path);
        return true;
    }

    public DateTimeOffset GetFileCreationTime(string path) =>
        MockFileSystem.FileInfo.New(path).CreationTimeUtc;

    public string ReadAllTextFromFile(string file) => MockFileSystem.File.ReadAllText(file);

    public Stream OpenFileForReading(string path) => MockFileSystem.File.OpenRead(path);

    public Stream CreateFileForWriting(string path)
    {
        return MockFileSystem.File.Create(path);
    }

    public bool WriteAllTextToFile(string path, string contents)
    {
        MockFileSystem.File.WriteAllText(path, contents);
        return true;
    }
}

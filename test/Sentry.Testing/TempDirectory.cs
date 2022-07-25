namespace Sentry.Testing;

public class TempDirectory : IDisposable
{
    private readonly IFileSystem _fileSystem;
    public string Path { get; }

    public TempDirectory(string path = default) : this(default, path)
    {
    }

    internal TempDirectory(IFileSystem fileSystem, string path = default)
    {
        _fileSystem = fileSystem ?? new FileSystem();
        Path = path ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
        _fileSystem.CreateDirectory(Path);
    }

    public void Dispose()
    {
        if (_fileSystem.DirectoryExists(Path))
        {
            _fileSystem.DeleteDirectory(Path, true);
        }
    }
}

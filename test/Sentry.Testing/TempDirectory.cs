namespace Sentry.Testing;

public class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory(string path)
    {
        Path = path;
        Directory.CreateDirectory(path);
    }

    public TempDirectory()
        : this(System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString()))
    { }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, true);
        }
    }
}

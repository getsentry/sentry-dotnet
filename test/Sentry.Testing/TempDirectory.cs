namespace Sentry.Testing;

public class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory(string path = default)
    {
        Path = path ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, true);
        }
    }
}

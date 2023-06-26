namespace Sentry.Tests.Internals;

public class FileSystemTests : IDisposable
{
    private readonly string _filename;

    public FileSystemTests()
    {
        _filename = Guid.NewGuid().ToString();
    }

    // [Fact]
    // public void Leased_file_cannot_be_leased_again()
    // {
    //     const string path = @$"C:\test\{Filename}";
    //
    //     IDictionary<string, MockFileData> files  = new Dictionary<string, MockFileData>
    //     {
    //         { path, new MockFileData(Filename) { AllowedFileShare = FileShare.None } }
    //     };
    //
    //     IFileSystem fileSystem = new FakeFileSystem(files);
    //
    //     Assert.Throws<IOException>(() =>
    //     {
    //         using (fileSystem.GetLeaseFile(path)) { }
    //     });
    // }

    [Fact]
    public void RealFileSystem_Leased_file_is_accessible_after_process_crush()
    {
        FileSystem.Instance.DeleteFile(_filename);

        var thread = new Thread(() =>
        {
            _ = FileSystem.Instance.GetLeaseFile(_filename);
        });

        thread.Start();
        thread.Join(200);

        var keepTrying = true;

        while (keepTrying)
        {
            Thread.Sleep(200);

            try
            {
                _ = FileSystem.Instance.GetLeaseFile(_filename);

                keepTrying = false;
            }
            catch (IOException)
            {
                // Immediate access might fail, retry
            }
        }
    }

    public void Dispose()
    {
        try
        {
            FileSystem.Instance.DeleteFile(_filename);
        }
        catch
        {
            //
        }
    }
}

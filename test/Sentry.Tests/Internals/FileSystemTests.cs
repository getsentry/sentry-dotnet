using System.IO.Abstractions.TestingHelpers;

namespace Sentry.Tests.Internals;

public class FileSystemTests
{
    private const string Filename = "lock.txt";

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
        FileSystem.Instance.DeleteFile(Filename);

        var thread = new Thread(() =>
        {
            _ = FileSystem.Instance.GetLeaseFile(Filename);
        });

        thread.Start();
        thread.Join(200);

        var keepTrying = true;

        while (keepTrying)
        {
            Thread.Sleep(200);

            try
            {
                _ = FileSystem.Instance.GetLeaseFile(Filename);

                keepTrying = false;
            }
            catch (IOException)
            {
                // Immediate access might fail, retry
            }
        }
    }
}

using System.IO.Abstractions.TestingHelpers;

namespace Sentry.Tests.Internals;

public class FileSystemTests
{
    private const string Filename = "lock.txt";

    [Fact(Skip = "Run manually if needed")]
    public void Can_get_lease_after_it_was_consumed()
    {
        FileSystem.Instance.DeleteFile(Filename);

        _ = FileSystem.Instance.GetLeaseFile(Filename);

        Assert.Throws<IOException>(() =>
        {
            using (FileSystem.Instance.GetLeaseFile(Filename)) { }
        });
    }

    [Fact]
    public void Leased_file_cannot_be_leased_again()
    {
        const string path = @$"C:\test\{Filename}";

        IDictionary<string, MockFileData> files  = new Dictionary<string, MockFileData>
        {
            { path, new MockFileData(Filename) { AllowedFileShare = FileShare.None } }
        };

        IFileSystem fileSystem = new FakeFileSystem(files);

        Assert.Throws<IOException>(() =>
        {
            using (fileSystem.GetLeaseFile(path)) { }
        });
    }
}

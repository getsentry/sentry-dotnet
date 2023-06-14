namespace Sentry.Tests.Internals;

public class FileSystemTests
{
    [Fact]
    public async Task Can_get_lease_after_it_was_consumed()
    {
        FileSystem.Instance.DeleteFile("lock.txt");

        await Task.Run(() => _ = FileSystem.Instance.GetLeaseFile("lock.txt"));

        using (FileSystem.Instance.GetLeaseFile("lock.txt")) { }
    }
}

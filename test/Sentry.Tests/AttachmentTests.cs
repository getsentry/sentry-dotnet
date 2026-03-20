namespace Sentry.Tests;

public class ByteAttachmentContentTests
{
    [Fact]
    public void Bytes_ReturnsConstructorValue()
    {
        var data = new byte[] { 1, 2, 3 };
        var content = new ByteAttachmentContent(data);
        Assert.Same(data, content.Bytes);
    }

    [Fact]
    public void GetStream_ReturnsBytesContent()
    {
        var data = new byte[] { 10, 20, 30 };
        var content = new ByteAttachmentContent(data);

        using var stream = content.GetStream();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);

        Assert.Equal(data, ms.ToArray());
    }
}

public class FileAttachmentContentTests
{
    [Fact]
    public void FilePath_ReturnsConstructorValue()
    {
        var attachment = new FileAttachmentContent("/some/path/file.txt");
        Assert.Equal("/some/path/file.txt", attachment.FilePath);
    }

    [Fact]
    public void DoesNotLock()
    {
        // Arrange
        using var tempDir = new TempDirectory();
        var filePath = Path.Combine(tempDir.Path, "MyFile.txt");
        File.WriteAllText(filePath, "Hello world!");

        var attachment = new FileAttachmentContent(filePath);
        // Act
        using (var stream = attachment.GetStream())
        {
            Assert.False(IsFileLocked(filePath));
        }
    }

    private static bool IsFileLocked(string file)
    {
        try
        {
            using FileStream stream = new(file, FileMode.Open, FileAccess.ReadWrite);
            stream.Close();
        }
        catch (IOException)
        {
            //the file is unavailable because it is:
            //still being written to
            //or being processed by another thread
            //or does not exist (has already been processed)
            return true;
        }

        //file is not locked
        return false;
    }
}

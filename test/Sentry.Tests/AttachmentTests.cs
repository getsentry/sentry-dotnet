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

public class ViewHierarchyAttachmentTests
{
    [Fact]
    public void Ctor_DefaultsAddToTransactionsToFalse()
    {
        var attachment = new ViewHierarchyAttachment(new ByteAttachmentContent(new byte[] { 1 }));
        Assert.False(attachment.AddToTransactions);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Ctor_ForwardsAddToTransactions(bool addToTransactions)
    {
        var attachment = new ViewHierarchyAttachment(
            new ByteAttachmentContent(new byte[] { 1 }),
            addToTransactions);

        Assert.Equal(addToTransactions, attachment.AddToTransactions);
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

public class GzipFileAttachmentContentTests
{
    [Fact]
    public void Ctor_IsFileAttachmentContent_SoObserversGetTheRawPath()
    {
        var content = new GzipFileAttachmentContent("/some/path/file.txt");

        content.Should().BeAssignableTo<FileAttachmentContent>();
        content.FilePath.Should().Be("/some/path/file.txt");
        content.CompressionLevel.Should().Be(System.IO.Compression.CompressionLevel.Optimal);
    }

    [Fact]
    public void GetStream_RoundTripsThroughGzip()
    {
        // Arrange
        using var tempDir = new TempDirectory();
        var filePath = Path.Combine(tempDir.Path, "MyFile.txt");
        var text = string.Concat(Enumerable.Repeat("Hello world! ", 50_000));
        File.WriteAllText(filePath, text);
        var content = new GzipFileAttachmentContent(filePath, System.IO.Compression.CompressionLevel.Fastest);

        // Act
        using var compressed = new MemoryStream();
        using (var stream = content.GetStream())
        {
            stream.CanSeek.Should().BeFalse();
            stream.CopyTo(compressed);
        }

        // Assert
        compressed.Length.Should().BeLessThan(text.Length / 10);
        compressed.Position = 0;
        using var gunzip = new System.IO.Compression.GZipStream(compressed, System.IO.Compression.CompressionMode.Decompress);
        using var reader = new StreamReader(gunzip);
        reader.ReadToEnd().Should().Be(text);
    }

    [Fact]
    public void GetStream_DoesNotLockFile()
    {
        // Arrange
        using var tempDir = new TempDirectory();
        var filePath = Path.Combine(tempDir.Path, "MyFile.txt");
        File.WriteAllText(filePath, "Hello world!");
        var content = new GzipFileAttachmentContent(filePath);

        // Act
        using var stream = content.GetStream();

        // Assert
        using var writer = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        writer.CanWrite.Should().BeTrue();
    }

    [Fact]
    public void GetStream_MissingFile_Throws()
    {
        var content = new GzipFileAttachmentContent(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        Assert.Throws<FileNotFoundException>(() => content.GetStream());
    }
}

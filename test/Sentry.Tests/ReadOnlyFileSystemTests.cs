namespace Sentry.Tests;

public class ReadOnlyFileSystemTests
{
    private readonly ReadOnlyFileSystem _sut = new();

    [Fact]
    public void CreateDirectory_ReturnsFileOperationResultDisabled() =>
        Assert.False(_sut.CreateDirectory("someDirectory"));

    [Fact]
    public void DeleteDirectory_ReturnsFileOperationResultDisabled() =>
        Assert.False(_sut.DeleteDirectory("someDirectory"));

    [Fact]
    public void CreateFileForWriting_ReturnsFileOperationResultDisabledAndNullStream()
    {
        Assert.False(_sut.CreateFileForWriting("someFile", out var fileStream));
        Assert.Equal(Stream.Null, fileStream);
    }

    [Fact]
    public void WriteAllTextToFile_ReturnsFileOperationDisabled() =>
        Assert.False(_sut.WriteAllTextToFile("someFile", "someContent"));

    [Fact]
    public void MoveFile_ReturnsFileOperationDisabled() =>
        Assert.False(_sut.MoveFile("someSourceFile", "someDestinationFile"));

    [Fact]
    public void DeleteFile_ReturnsFileOperationResultDisabled() =>
        Assert.False(_sut.DeleteFile("someFile"));
}

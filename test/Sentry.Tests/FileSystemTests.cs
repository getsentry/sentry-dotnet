namespace Sentry.Tests;

public class FileSystemTests
{
    private class Fixture
    {
        public SentryOptions Options;

        public Fixture() => Options = new SentryOptions { Dsn = ValidDsn };

        public FileSystem GetSut() => new(Options);
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void CreateDirectory_FileWriteDisabled_ReturnsFalse()
    {
        _fixture.Options.DisableFileWrite = true;
        Assert.False(_fixture.GetSut().CreateDirectory("SomeDirectory"));
    }

    [Fact]
    public void DeleteDirectory_FileWriteDisabled_ReturnsFalse()
    {
        _fixture.Options.DisableFileWrite = true;
        Assert.False(_fixture.GetSut().DeleteDirectory("SomeDirectory"));
    }

    [Fact]
    public void MoveFile_FileWriteDisabled_ReturnsFalse()
    {
        _fixture.Options.DisableFileWrite = true;
        Assert.False(_fixture.GetSut().MoveFile("source", "destination"));
    }

    [Fact]
    public void DeleteFile_FileWriteDisabled_ReturnsFalse()
    {
        _fixture.Options.DisableFileWrite = true;
        Assert.False(_fixture.GetSut().DeleteFile("someFile"));
    }

    [Fact]
    public void CreateFileForWriting_FileWriteDisabled_ReturnsNullStream()
    {
        _fixture.Options.DisableFileWrite = true;

        var fileStream = _fixture.GetSut().CreateFileForWriting("someFile");
        Assert.Equal(Stream.Null, fileStream);
    }

    [Fact]
    public void WriteAllTextToFile_FileWriteDisabled_ReturnsFalse()
    {
        _fixture.Options.DisableFileWrite = true;
        Assert.False(_fixture.GetSut().WriteAllTextToFile("someFile", "someContent"));
    }
}

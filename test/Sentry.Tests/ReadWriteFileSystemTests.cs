namespace Sentry.Tests;

public class ReadWriteFileSystemTests
{
    private readonly ReadWriteFileSystem _sut = new();

    [Fact]
    public void CreateDirectory_CreatesDirectoryAndReturnsSuccess()
    {
        using var tempDirectory = new TempDirectory();
        var directoryPath = Path.Combine(tempDirectory.Path, "someDirectory");

        var result = _sut.CreateDirectory(directoryPath);

        Assert.Equal(FileOperationResult.Success, result);
        Assert.True(Directory.Exists(directoryPath));
    }

    [Fact]
    public void DeleteDirectory_DeletesDirectoryAndReturnsSuccess()
    {
        // Arrange
        using var tempDirectory = new TempDirectory();
        var directoryPath = Path.Combine(tempDirectory.Path, "someDirectory");

        _sut.CreateDirectory(directoryPath);
        Assert.True(Directory.Exists(directoryPath)); // Sanity check

        // Act
        var result = _sut.DeleteDirectory(directoryPath);

        // Assert
        Assert.Equal(FileOperationResult.Success, result);
        Assert.False(Directory.Exists(directoryPath));
    }

    [Fact]
    public void CreateFileForWriting_CreatesFileAndReturnsSuccess()
    {
        using var tempDirectory = new TempDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "someFile.txt");

        var result = _sut.CreateFileForWriting(filePath, out var fileStream);
        fileStream.Dispose();

        // Assert
        Assert.Equal(FileOperationResult.Success, result);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public void WriteAllTextToFile_CreatesFileAndReturnsSuccess()
    {
        using var tempDirectory = new TempDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "someFile.txt");
        var content = "someContent";

        var result = _sut.WriteAllTextToFile(filePath, content);

        // Assert
        Assert.Equal(FileOperationResult.Success, result);
        Assert.True(File.Exists(filePath));
        Assert.Equal(content, _sut.ReadAllTextFromFile(filePath));
    }

    [Fact]
    public void MoveFile_DestinationDoesNotExist_MovesFileAndReturnsSuccess()
    {
        // Arrange
        using var tempDirectory = new TempDirectory();
        var sourcePath = Path.Combine(tempDirectory.Path, "someSourceFile.txt");
        var destinationPath = Path.Combine(tempDirectory.Path, "someDestinationFile.txt");
        var content = "someContent";

        _sut.WriteAllTextToFile(sourcePath, content);
        Assert.True(File.Exists(sourcePath)); // Sanity check

        // Act
        var result = _sut.MoveFile(sourcePath, destinationPath);

        // Assert
        Assert.Equal(FileOperationResult.Success, result);
        Assert.True(File.Exists(destinationPath));
        Assert.False(File.Exists(sourcePath));
    }

    [Fact]
    public void DeleteFile_ReturnsFileOperationResultDisabled()
    {
        // Arrange
        using var tempDirectory = new TempDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "someFile.txt");

        _sut.WriteAllTextToFile(filePath, "content");
        Assert.True(File.Exists(filePath)); // Sanity check

        // Act
        var result = _sut.DeleteFile(filePath);

        // Assert
        Assert.Equal(FileOperationResult.Success, result);
        Assert.False(File.Exists(filePath));
    }
}

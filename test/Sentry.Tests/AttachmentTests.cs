using Sentry.Testing;

namespace Sentry.Tests;

public class FileAttachmentContentTests
{
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

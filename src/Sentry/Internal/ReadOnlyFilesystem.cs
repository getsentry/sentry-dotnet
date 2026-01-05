namespace Sentry.Internal;

internal class ReadOnlyFileSystem : FileSystemBase
{
    // Note: You are responsible for handling success/failure when attempting to write to disk.
    // You are required to check for `Options.FileWriteDisabled` whether you are allowed to call any writing operations.
    // The options will automatically pick between `ReadOnly` and `ReadAndWrite` to prevent accidental file writing that
    // could cause crashes on restricted platforms like the Nintendo Switch.

    public override bool CreateDirectory(string path) => false;

    public override bool DeleteDirectory(string path, bool recursive = false) => false;

    public override bool CreateFileForWriting(string path, out Stream fileStream)
    {
        fileStream = Stream.Null;
        return false;
    }

    public override bool WriteAllTextToFile(string path, string contents) => false;

    public override bool MoveFile(string sourceFileName, string destFileName, bool overwrite = false) => false;

    public override bool DeleteFile(string path) => false;
}

namespace Sentry.Internal;

internal class ReadOnlyFileSystem : FileSystemBase
{
    public override FileOperationResult CreateDirectory(string path) => FileOperationResult.Disabled;

    public override FileOperationResult DeleteDirectory(string path, bool recursive = false) => FileOperationResult.Disabled;

    public override FileOperationResult CreateFileForWriting(string path, out Stream fileStream)
    {
        fileStream = Stream.Null;
        return FileOperationResult.Disabled;
    }

    public override FileOperationResult WriteAllTextToFile(string path, string contents) => FileOperationResult.Disabled;

    public override FileOperationResult MoveFile(string sourceFileName, string destFileName, bool overwrite = false) =>
        FileOperationResult.Disabled;

    public override FileOperationResult DeleteFile(string path) => FileOperationResult.Disabled;
}

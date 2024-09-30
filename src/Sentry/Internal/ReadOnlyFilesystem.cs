namespace Sentry.Internal;

internal class ReadOnlyFileSystem : FileSystemBase
{
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

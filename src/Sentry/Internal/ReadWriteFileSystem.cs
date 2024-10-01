namespace Sentry.Internal;

internal class ReadWriteFileSystem : FileSystemBase
{
    // Note: You are responsible for handling success/failure when attempting to write to disk.
    // You are required to check for `Options.FileWriteDisabled` whether you are allowed to call any writing operations.
    // The options will automatically pick between `ReadOnly` and `ReadAndWrite` to prevent accidental file writing that
    // could cause crashes on restricted platforms like the Nintendo Switch.

    public override bool CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return DirectoryExists(path);
    }

    public override bool DeleteDirectory(string path, bool recursive = false)
    {
        Directory.Delete(path, recursive);
        return !Directory.Exists(path);
    }

    public override bool CreateFileForWriting(string path, out Stream fileStream)
    {
        fileStream = File.Create(path);
        return true;
    }

    public override bool WriteAllTextToFile(string path, string contents)
    {
        File.WriteAllText(path, contents);
        return File.Exists(path);
    }

    public override bool MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
    {
#if NETCOREAPP3_0_OR_GREATER
        File.Move(sourceFileName, destFileName, overwrite);
#else
        if (overwrite)
        {
            File.Copy(sourceFileName, destFileName, overwrite: true);
            File.Delete(sourceFileName);
        }
        else
        {
            File.Move(sourceFileName, destFileName);
        }
#endif

        if (File.Exists(sourceFileName) || !File.Exists(destFileName))
        {
            return false;
        }

        return true;
    }

    public override bool DeleteFile(string path)
    {
        File.Delete(path);
        return !File.Exists(path);
    }
}

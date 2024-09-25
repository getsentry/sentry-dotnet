namespace Sentry.Internal;

internal class ReadWriteFileSystem : FileSystemBase
{
    public override FileOperationResult CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return DirectoryExists(path) ? FileOperationResult.Success : FileOperationResult.Failure;
    }

    public override FileOperationResult DeleteDirectory(string path, bool recursive = false)
    {
        Directory.Delete(path, recursive);
        return Directory.Exists(path) ? FileOperationResult.Failure : FileOperationResult.Success;
    }

    public override FileOperationResult CreateFileForWriting(string path, out Stream fileStream)
    {
        fileStream = File.Create(path);
        return FileOperationResult.Success;
    }

    public override FileOperationResult WriteAllTextToFile(string path, string contents)
    {
        File.WriteAllText(path, contents);
        return File.Exists(path) ? FileOperationResult.Success : FileOperationResult.Failure;
    }

    public override FileOperationResult MoveFile(string sourceFileName, string destFileName, bool overwrite = false)
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
            return FileOperationResult.Failure;
        }

        return FileOperationResult.Success;
    }

    public override FileOperationResult DeleteFile(string path)
    {
        File.Delete(path);
        return File.Exists(path) ? FileOperationResult.Failure : FileOperationResult.Success;
    }
}

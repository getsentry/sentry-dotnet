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

    /// <summary>
    /// Tries to create or open a file for exclusive access.
    /// </summary>
    /// <remarks>
    /// This method can throw all of the same exceptions that the <see cref="FileStream"/> constructor can throw. The
    /// caller is responsible for handling those exceptions.
    /// </remarks>
    public override bool TryCreateLockFile(string path, out Stream fileStream)
    {
        // Note that FileShare.None is implemented via advisory locks only on macOS/Linux... so it will stop
        // other .NET processes from accessing the file but not other non-.NET processes. This should be fine
        // in our case - we just want to avoid multiple instances of the SDK concurrently accessing the cache
        fileStream = new FileStream(
            path,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None);
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

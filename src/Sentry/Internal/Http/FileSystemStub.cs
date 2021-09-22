using System.Collections.Generic;
using System.IO;

namespace Sentry.Internal.Http
{
    internal class FileSystemStub : IFileSystem
    {
        public static IFileSystem Instance { get; } = new FileSystemStub();

        public virtual Stream OpenReadFile(string path) => File.OpenRead(path);
        public virtual Stream CreateFile(string path) => File.Create(path);

        public virtual void MoveFile(string filePath, string targetFilePath)
        {
#if NETCOREAPP3_0_OR_GREATER
            File.Move(filePath, targetFilePath, true);
#else
            File.Copy(filePath, targetFilePath, true);
            File.Delete(filePath);
#endif
        }

        public virtual void DeleteFile(string fileName) => File.Delete(fileName);
        public virtual void DeleteDirectory(string fileName) => Directory.Delete(fileName, true);
        public virtual void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public virtual IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return Directory.EnumerateDirectories(path, searchPattern, searchOption);
        }

        public virtual IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return Directory.EnumerateFiles(path, searchPattern, searchOption);
        }

        public virtual bool DirectoryExists(string path) => Directory.Exists(path);
    }
}

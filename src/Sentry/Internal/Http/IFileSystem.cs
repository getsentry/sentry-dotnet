using System.Collections.Generic;
using System.IO;

namespace Sentry.Internal.Http
{
    internal interface IFileSystem
    {
        Stream OpenReadFile(string path);
        Stream CreateFile(string path);
        void MoveFile(string filePath, string targetFilePath);
        void DeleteFile(string fileName);
        void DeleteDirectory(string fileName);
        void CreateDirectory(string path);
        IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
        IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);
        bool DirectoryExists(string path);
    }
}

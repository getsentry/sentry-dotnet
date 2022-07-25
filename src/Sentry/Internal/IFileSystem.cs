using System;
using System.Collections.Generic;
using System.IO;

namespace Sentry.Internal
{
    internal interface IFileSystem
    {
        IEnumerable<string> EnumerateFiles(string path);
        IEnumerable<string> EnumerateFiles(string path, string searchPattern);
        void CreateDirectory(string path);
        void DeleteDirectory(string path, bool recursive = false);
        bool DirectoryExists(string path);
        bool FileExists(string path);
        void MoveFile(string sourceFileName, string destFileName, bool overwrite = false);
        void DeleteFile(string path);
        DateTimeOffset GetFileCreationTime(string path);
        string ReadAllTextFromFile(string file);
        Stream OpenFileForReading(string path);
        Stream CreateFileForWriting(string path);
    }
}

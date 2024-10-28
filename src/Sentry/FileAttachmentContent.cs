using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Attachment sourced from the file system.
/// </summary>
public class FileAttachmentContent : IAttachmentContent
{
    private readonly string _filePath;
    private readonly bool _readFileAsynchronously;

    /// <summary>
    /// Creates a new instance of <see cref="FileAttachmentContent"/>.
    /// </summary>
    /// <param name="filePath">The path to the file to attach.</param>
    public FileAttachmentContent(string filePath) : this(filePath, true)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="FileAttachmentContent"/>.
    /// </summary>
    /// <param name="filePath">The path to the file to attach.</param>
    /// <param name="readFileAsynchronously">Whether to use async file I/O to read the file.</param>
    public FileAttachmentContent(string filePath, bool readFileAsynchronously)
    {
        _filePath = filePath;
        _readFileAsynchronously = readFileAsynchronously;
    }

    /// <inheritdoc />
    public Stream GetStream() => new FileStream(
        _filePath,
        FileMode.Open,
        FileAccess.Read,
        FileShare.ReadWrite,
        bufferSize: 4096,
        useAsync: _readFileAsynchronously);
}

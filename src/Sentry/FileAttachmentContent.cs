using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Attachment sourced from the file system.
/// </summary>
public class FileAttachmentContent : IAttachmentContent
{
    private readonly bool _readFileAsynchronously;

    private readonly bool _deleteOnClose;

    /// <summary>
    /// The path to the file to attach.
    /// </summary>
    internal string FilePath { get; }

    /// <summary>
    /// Creates a new instance of <see cref="FileAttachmentContent"/>.
    /// </summary>
    /// <param name="filePath">The path to the file to attach.</param>
    public FileAttachmentContent(string filePath) : this(filePath, true, false)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="FileAttachmentContent"/>.
    /// </summary>
    /// <param name="filePath">The path to the file to attach.</param>
    /// <param name="readFileAsynchronously">Whether to use async file I/O to read the file.</param>
    public FileAttachmentContent(string filePath, bool readFileAsynchronously) : this(filePath, readFileAsynchronously, false)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="FileAttachmentContent"/>.
    /// </summary>
    /// <param name="filePath">The path to the file to attach.</param>
    /// <param name="readFileAsynchronously">Whether to use async file I/O to read the file.</param>
    /// <param name="deleteOnClose">Whether to delete the file when it closed.</param>
    public FileAttachmentContent(string filePath, bool readFileAsynchronously, bool deleteOnClose)
    {
        FilePath = filePath;
        _readFileAsynchronously = readFileAsynchronously;
        _deleteOnClose = deleteOnClose;
    }

    /// <inheritdoc />
    public Stream GetStream()
    {
        var options = FileOptions.None;

        if (_readFileAsynchronously)
            options |= FileOptions.Asynchronous;

        if (_deleteOnClose)
            options |= FileOptions.DeleteOnClose;

        return new FileStream(
            FilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 4096,
            options);
    }
}

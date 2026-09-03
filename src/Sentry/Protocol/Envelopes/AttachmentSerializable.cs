using Sentry.Extensibility;

namespace Sentry.Protocol.Envelopes;

/// <summary>
/// Represents attachment content that obtains a stream for each serialization.
/// </summary>
internal sealed class AttachmentSerializable : ISerializable
{
    private readonly IAttachmentContent _content;

    /// <summary>
    /// Initializes an instance of <see cref="AttachmentSerializable"/>.
    /// </summary>
    /// <param name="content">The attachment content to serialize.</param>
    public AttachmentSerializable(IAttachmentContent content) => _content = content;

    /// <inheritdoc />
    public async Task SerializeAsync(Stream stream, IDiagnosticLogger? logger, CancellationToken cancellationToken = default)
    {
        using var contentStream = _content.GetStream();
        await contentStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Serialize(Stream stream, IDiagnosticLogger? logger)
    {
        using var contentStream = _content.GetStream();
        contentStream.CopyTo(stream);
    }
}

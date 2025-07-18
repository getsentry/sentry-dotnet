namespace Sentry.Testing;

internal sealed class NullAttachmentContent : IAttachmentContent
{
    public static NullAttachmentContent Instance { get; } = new();

    public Stream GetStream() => Stream.Null;
}

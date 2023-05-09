namespace Sentry.Tests
{
    internal static class AttachmentHelper
    {
        internal static Attachment FakeAttachment(string name = "test.txt")
        => new(
            AttachmentType.Default,
            new StreamAttachmentContent(new MemoryStream(new byte[] { 1 })),
            name,
            null
            );
    }
}

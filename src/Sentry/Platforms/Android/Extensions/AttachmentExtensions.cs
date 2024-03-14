namespace Sentry.Android.Extensions;

internal static class AttachmentExtensions
{
    public static SentryAttachment ToAttachment(this JavaSdk.Attachment attachment)
    {
        // TODO: Convert JavaSdk.Attachment to Sentry.Attachment.
        // One way to do this might be to serialise the JavaSdk.Attachment as
        // JSON and then deserialise it as a Sentry.Attachment. It looks like
        // Attachments aren't designed to be serialised directly though (they
        // get stuffed into EnvelopeItems instead)... and I'm not sure if we'd
        // have access to the JSON serialiser from here or how the data in 
        // JavaSdk.Attachment.GetBytes() is encoded.
        throw new NotImplementedException();
    }

    public static JavaSdk.Attachment ToJavaAttachment(this SentryAttachment attachment)
    {
        // TODO: Convert Sentry.Attachment to JavaSdk.Attachment.
        // Same problem as ToAttachment() above but in reverse.
        throw new NotImplementedException();
    }
}

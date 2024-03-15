namespace Sentry.Android.Extensions;

internal static class HintExtensions
{
    public static SentryHint ToHint(this JavaSdk.Hint javaHint)
    {
        // Note the JavaSDK doesn't expose the internal hint storage in any way that is iterable,
        // so unless you know the key, you can't get the value. This prevents us from converting
        // anything in the JavaSdk.Hint except the explicitly named properties:
        //  Attachments, Screenshot and ViewHierarchy

        var dotnetHint = new SentryHint();
        // TODO: Implement ToAttachment
        //dotnetHint.Screenshot = (javaHint.Screenshot is { } screenshot) ? screenshot.ToAttachment() : null;
        //dotnetHint.ViewHierarchy = (javaHint.ViewHierarchy is { } viewhierarchy) ? viewhierarchy.ToAttachment() : null;
        //dotnetHint.AddAttachments(javaHint.Attachments.Select(x => x.ToAttachment()));

        return dotnetHint;
    }

    public static JavaSdk.Hint ToJavaHint(this SentryHint dotnetHint)
    {
        var javaHint = new JavaSdk.Hint();
        // TODO: Implement ToJavaAttachment
        //javaHint.Screenshot = (dotnetHint.Screenshot is { } screenshot) ? screenshot.ToJavaAttachment() : null;
        //javaHint.ViewHierarchy = (dotnetHint.ViewHierarchy is { } viewhierarchy) ? viewhierarchy.ToJavaAttachment() : null;
        //javaHint.AddAttachments(dotnetHint.Attachments.Select(x => x.ToJavaAttachment()).ToList());

        return javaHint;
    }
}

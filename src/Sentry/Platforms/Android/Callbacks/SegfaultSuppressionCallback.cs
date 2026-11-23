namespace Sentry.Android.Callbacks;

internal class SegfaultSuppressionCallback(JavaSdk.SentryOptions.IBeforeSendCallback? next)
    : JavaObject, JavaSdk.SentryOptions.IBeforeSendCallback
{
    public JavaSdk.SentryEvent? Execute(JavaSdk.SentryEvent e, JavaSdk.Hint h) =>
        e.Exceptions?.FirstOrDefault() is { Type: "SIGSEGV", Value: "Segfault" }
            ? null
            : next is null ? e : next.Execute(e, h);
}

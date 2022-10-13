using Sentry.Android.Extensions;
using Sentry.Extensibility;

namespace Sentry.Android.Callbacks;

internal class BeforeSendCallback : JavaObject, JavaSdk.SentryOptions.IBeforeSendCallback
{
    private readonly Func<SentryEvent, SentryEvent?> _beforeSend;
    private readonly SentryOptions _options;
    private readonly JavaSdk.SentryOptions _javaOptions;

    public BeforeSendCallback(
        Func<SentryEvent, SentryEvent?> beforeSend,
        SentryOptions options,
        JavaSdk.SentryOptions javaOptions)
    {
        _beforeSend = beforeSend;
        _options = options;
        _javaOptions = javaOptions;
    }

    public JavaSdk.SentryEvent? Execute(JavaSdk.SentryEvent e, JavaSdk.Hint h)
    {
        // Note: Hint is unused due to:
        // https://github.com/getsentry/sentry-dotnet/issues/1469

        var evnt = e.ToSentryEvent(_javaOptions);
        var result = _beforeSend.Invoke(evnt);
        return result?.ToJavaSentryEvent(_options, _javaOptions);
    }
}

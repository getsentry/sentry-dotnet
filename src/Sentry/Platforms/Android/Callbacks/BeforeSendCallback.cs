using Sentry.Android.Extensions;
using Sentry.Extensibility;

namespace Sentry.Android.Callbacks;

internal class BeforeSendCallback : JavaObject, JavaSdk.SentryOptions.IBeforeSendCallback
{
    private readonly Func<SentryEvent, SentryHint, SentryEvent?> _beforeSend;
    private readonly SentryOptions _options;
    private readonly JavaSdk.SentryOptions _javaOptions;

    public BeforeSendCallback(
        Func<SentryEvent, SentryHint, SentryEvent?> beforeSend,
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
        try
        {
            // because this can go out to user code, we want to prevent external crashing
            // also, native types tend to move before dotnet does, so serialization can fail
            var evnt = e.ToSentryEvent(_javaOptions);
            var hint = h.ToHint();
            var result = _beforeSend?.Invoke(evnt, hint);
            return result?.ToJavaSentryEvent(_options, _javaOptions);
        }
        catch (Exception exception)
        {
            _options.LogError(exception, "Before Send Error");
            return e;
        }
    }
}

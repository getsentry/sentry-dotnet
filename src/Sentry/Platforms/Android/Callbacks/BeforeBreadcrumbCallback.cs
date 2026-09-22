using Sentry.Android.Extensions;
using Sentry.Extensibility;

namespace Sentry.Android.Callbacks;

internal class BeforeBreadcrumbCallback : JavaObject, JavaSdk.SentryOptions.IBeforeBreadcrumbCallback
{
    private readonly Func<Breadcrumb, SentryHint, Breadcrumb?> _beforeBreadcrumb;
    private readonly SentryOptions _options;

    public BeforeBreadcrumbCallback(
        Func<Breadcrumb, SentryHint, Breadcrumb?> beforeBreadcrumb,
        SentryOptions options)
    {
        _beforeBreadcrumb = beforeBreadcrumb;
        _options = options;
    }

    public JavaSdk.Breadcrumb? Execute(JavaSdk.Breadcrumb b, JavaSdk.Hint h)
    {
        // Note: Hint is unused due to:
        // https://github.com/getsentry/sentry-dotnet/issues/1469

        var breadcrumb = b.ToBreadcrumb();
        var hint = h.ToHint();

        Breadcrumb? result;
        try
        {
            result = _beforeBreadcrumb.Invoke(breadcrumb, hint);
        }
        catch (Exception exception)
        {
            _options.LogError(exception, "BeforeBreadcrumb callback failed.");
            return null;
        }

        if (result == breadcrumb)
        {
            // The result is the same object as was input, and all properties are immutable,
            // so we can return the original Java object for better performance.
            return b!;
        }

        return result?.ToJavaBreadcrumb();
    }
}

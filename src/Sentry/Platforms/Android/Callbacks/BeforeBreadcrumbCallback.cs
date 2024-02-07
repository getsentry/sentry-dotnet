using Sentry.Android.Extensions;

namespace Sentry.Android.Callbacks;

internal class BeforeBreadcrumbCallback : JavaObject, JavaSdk.SentryOptions.IBeforeBreadcrumbCallback
{
    private readonly Func<Breadcrumb, SentryHint, Breadcrumb?> _beforeBreadcrumb;

    public BeforeBreadcrumbCallback(Func<Breadcrumb, SentryHint, Breadcrumb?> beforeBreadcrumb)
    {
        _beforeBreadcrumb = beforeBreadcrumb;
    }

    public JavaSdk.Breadcrumb? Execute(JavaSdk.Breadcrumb b, JavaSdk.Hint h)
    {
        // Note: Hint is unused due to:
        // https://github.com/getsentry/sentry-dotnet/issues/1469

        var breadcrumb = b.ToBreadcrumb();
        var hint = h.ToHint();
        var result = _beforeBreadcrumb.Invoke(breadcrumb, hint);

        if (result == breadcrumb)
        {
            // The result is the same object as was input, and all properties are immutable,
            // so we can return the original Java object for better performance.
            return b!;
        }

        return result?.ToJavaBreadcrumb();
    }
}

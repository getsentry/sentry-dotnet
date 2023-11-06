using Sentry.Android.Extensions;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Android;

internal sealed class AndroidScopeObserver : IScopeObserver
{
    private readonly SentryOptions _options;
    private readonly IScopeObserver? _innerObserver;

    public AndroidScopeObserver(SentryOptions options)
    {
        _options = options;

        // Chain any previous observer, but guard against circular reference.
        var observer = options.ScopeObserver;
        _innerObserver = observer is AndroidScopeObserver ? null : observer;
    }

    public void AddBreadcrumb(Breadcrumb breadcrumb)
    {
        try
        {
            var b = breadcrumb.ToJavaBreadcrumb();
            JavaSdk.Sentry.AddBreadcrumb(b);
        }
        finally
        {
            _innerObserver?.AddBreadcrumb(breadcrumb);
        }
    }

    public void SetExtra(string key, object? value)
    {
        try
        {
            if (value is null)
            {
                _options.LogDebug("Extra with key '{0}' was null.", key);
                return;
            }

            if (value is string s)
            {
                JavaSdk.Sentry.SetExtra(key, s);
                return;
            }

            try
            {
                var json = value.ToUtf8Json();
                JavaSdk.Sentry.SetExtra(key, json);
            }
            catch (Exception ex)
            {
                _options.LogError(ex, "Extra with key '{0}' could not be serialized.", key);
            }
        }
        finally
        {
            _innerObserver?.SetExtra(key, value);
        }
    }

    public void SetTag(string key, string value)
    {
        try
        {
            JavaSdk.Sentry.SetTag(key, value);
        }
        finally
        {
            _innerObserver?.SetTag(key, value);
        }
    }

    public void UnsetTag(string key)
    {
        try
        {
            JavaSdk.Sentry.RemoveTag(key);
        }
        finally
        {
            _innerObserver?.UnsetTag(key);
        }
    }

    public void UnsetExtra(string key)
    {
        try
        {
            JavaSdk.Sentry.RemoveExtra(key);
        }
        finally
        {
            _innerObserver?.UnsetExtra(key);
        }
    }

    public void SetUser(User? user)
    {
        try
        {
            var u = user?.ToJavaUser();
            JavaSdk.Sentry.SetUser(u);
        }
        finally
        {
            _innerObserver?.SetUser(user);
        }
    }
}

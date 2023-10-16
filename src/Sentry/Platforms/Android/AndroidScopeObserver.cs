using Sentry.Android.Extensions;
using Sentry.Extensibility;

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

    public void SetData(string key, object? value)
    {
        try
        {
            if (value is null)
            {
                _options.LogDebug("DataData with key '{0}' was null.", key);
                return;
            }

            if (value is string s)
            {
                JavaSdk.Sentry.SetExtra(key, s);
                return;
            }

            try
            {
                var json = JsonSerializer.Serialize(value);
                JavaSdk.Sentry.SetExtra(key, json);
            }
            catch (Exception ex)
            {
                _options.LogError(ex, "DataData with key '{0}' could not be serialized.", key);
            }
        }
        finally
        {
            _innerObserver?.SetData(key, value);
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

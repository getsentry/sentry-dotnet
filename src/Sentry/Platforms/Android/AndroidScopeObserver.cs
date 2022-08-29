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
            Java.Sentry.AddBreadcrumb(b);
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
                Java.Sentry.SetExtra(key, s);
                return;
            }

            try
            {
                var json = JsonSerializer.Serialize(value);
                Java.Sentry.SetExtra(key, json);
            }
            catch (Exception ex)
            {
                _options.LogError("Extra with key '{0}' could not be serialized.", ex, key);
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
            Java.Sentry.SetTag(key, value);
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
            Java.Sentry.RemoveTag(key);
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
            Java.Sentry.SetUser(u);
        }
        finally
        {
            _innerObserver?.SetUser(user);
        }
    }
}

using System.Text.Json;
using Sentry.iOS.Extensions;
using Sentry.Extensibility;

namespace Sentry.iOS;

internal sealed class IosScopeObserver : IScopeObserver
{
    private readonly SentryOptions _options;
    private readonly IScopeObserver? _innerObserver;

    public IosScopeObserver(SentryOptions options)
    {
        _options = options;

        // Chain any previous observer, but guard against circular reference.
        var observer = options.ScopeObserver;
        _innerObserver = observer is IosScopeObserver ? null : observer;
    }

    public void AddBreadcrumb(Breadcrumb breadcrumb)
    {
        try
        {
            var b = breadcrumb.ToCocoaBreadcrumb();
            SentryCocoa.SentrySDK.ConfigureScope(scope => scope.AddBreadcrumb(b));
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
                SentryCocoa.SentrySDK.ConfigureScope(scope =>
                    scope.SetExtraValue(NSObject.FromObject(s), key));

                return;
            }

            try
            {
                var json = JsonSerializer.Serialize(value);
                SentryCocoa.SentrySDK.ConfigureScope(scope => scope.SetExtraValue(NSObject.FromObject(json), key));
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
            SentryCocoa.SentrySDK.ConfigureScope(scope => scope.SetTagValue(value, key));
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
            SentryCocoa.SentrySDK.ConfigureScope(scope => scope.RemoveTagForKey(key));
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
            var u = user?.ToCocoaUser();
            SentryCocoa.SentrySDK.ConfigureScope(scope => scope.SetUser(u));
        }
        finally
        {
            _innerObserver?.SetUser(user);
        }
    }
}

using Sentry.Cocoa.Extensions;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Cocoa;

internal sealed class CocoaScopeObserver : IScopeObserver
{
    private readonly SentryOptions _options;
    private readonly IScopeObserver? _innerObserver;

    public CocoaScopeObserver(SentryOptions options)
    {
        _options = options;

        // Chain any previous observer, but guard against circular reference.
        var observer = options.ScopeObserver;
        _innerObserver = observer is CocoaScopeObserver ? null : observer;
    }

    public void AddBreadcrumb(Breadcrumb breadcrumb)
    {
        try
        {
            var b = breadcrumb.ToCocoaBreadcrumb();
            CocoaScope.Configure(scope => scope.AddBreadcrumb(b), _options);
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
                CocoaScope.Configure(scope => scope.SetExtraValue(NSObject.FromObject(s), key), _options);

                return;
            }

            try
            {
                var json = value.ToUtf8Json();
                CocoaScope.Configure(scope => scope.SetExtraValue(NSObject.FromObject(json), key), _options);
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
        if (value is null)
        {
            _options.LogDebug("Tag with key '{0}' was null. Use Unset to remove tags instead.", key);
            return;
        }

        try
        {
            CocoaScope.Configure(scope => scope.SetTagValue(value, key), _options);
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
            CocoaScope.Configure(scope => scope.RemoveTagForKey(key), _options);
        }
        finally
        {
            _innerObserver?.UnsetTag(key);
        }
    }

    public void SetUser(SentryUser? user)
    {
        try
        {
            var u = user?.ToCocoaUser();
            CocoaScope.Configure(scope => scope.SetUser(u), _options);
        }
        finally
        {
            _innerObserver?.SetUser(user);
        }
    }

    public void SetTrace(SentryId traceId, SpanId parentSpanId)
    {
        try
        {
            SentryCocoaHybridSdk.Internal.SetTrace(traceId.ToCocoaObjCId(), parentSpanId.ToCocoaObjCSpanId());
        }
        finally
        {
            _innerObserver?.SetTrace(traceId, parentSpanId);
        }
    }

    public void SetEnvironment(string? environment)
    {
        try
        {
            CocoaScope.Configure(scope => scope.SetEnvironment(environment), _options);
        }
        finally
        {
            _innerObserver?.SetEnvironment(environment);
        }
    }

    public void AddAttachment(SentryAttachment attachment)
    {
        try
        {
            // TODO: Missing corresponding functionality on the Cocoa SDK
        }
        finally
        {
            _innerObserver?.AddAttachment(attachment);
        }
    }

    public void ClearAttachments()
    {
        try
        {
            // TODO: Missing corresponding functionality on the Cocoa SDK
        }
        finally
        {
            _innerObserver?.ClearAttachments();
        }
    }
}

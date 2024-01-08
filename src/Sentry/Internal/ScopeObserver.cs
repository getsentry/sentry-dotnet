using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internal;

/// <summary>
/// Scope Observer wrapper for the common behaviour accross platforms.
/// </summary>
internal abstract class ScopeObserver : Sentry.IScopeObserver
{
    private readonly SentryOptions _options;
    private readonly string _name;

    public ScopeObserver(
        string name, SentryOptions options)
    {
        _name = name;
        _options = options;
    }

    public void AddBreadcrumb(Breadcrumb breadcrumb)
    {
        _options.DiagnosticLogger?.Log(SentryLevel.Debug,
            "{0} Scope Sync - Adding breadcrumb m:\"{1}\" l:\"{2}\"", null, _name,
            breadcrumb.Message, breadcrumb.Level);
        AddBreadcrumbImpl(breadcrumb);
    }

    public abstract void AddBreadcrumbImpl(Breadcrumb breadcrumb);

    public void SetExtra(string key, object? value)
    {
        var serialized = value is null ? null : value.ToUtf8Json();
        if (value is not null && serialized is null)
        {
            _options.DiagnosticLogger?.Log(SentryLevel.Warning,
                "{0} Scope Sync - SetExtra k:\"{1}\" v:\"{2}\" - value was serialized as null",
                null, _name, key, value);
        }
        else
        {
            _options.DiagnosticLogger?.Log(SentryLevel.Debug,
                "{0} Scope Sync - Setting Extra k:\"{1}\" v:\"{2}\"", null, _name, key, value);
        }
        SetExtraImpl(key, serialized);
    }

    public abstract void SetExtraImpl(string key, string? value);

    public void SetTag(string key, string value)
    {
        _options.DiagnosticLogger?.Log(SentryLevel.Debug,
            "{0} Scope Sync - Setting Tag k:\"{1}\" v:\"{2}\"", null, _name, key, value);
        SetTagImpl(key, value);
    }

    public abstract void SetTagImpl(string key, string value);

    public void UnsetTag(string key)
    {
        _options.DiagnosticLogger?.Log(
            SentryLevel.Debug, "{0} Scope Sync - Unsetting Tag k:\"{1}\"", null, _name, key);
        UnsetTagImpl(key);
    }

    public abstract void UnsetTagImpl(string key);

    public void SetUser(SentryUser? user)
    {
        if (user is null)
        {
            _options.DiagnosticLogger?.Log(
                SentryLevel.Debug, "{0} Scope Sync - Unsetting User", null, _name);
            UnsetUserImpl();
        }
        else
        {
            _options.DiagnosticLogger?.Log(SentryLevel.Debug,
                "{0} Scope Sync - Setting User i:\"{1}\" n:\"{2}\"", null, _name, user.Id,
                user.Username);
            SetUserImpl(user);
        }
    }

    public abstract void SetUserImpl(SentryUser user);

    public abstract void UnsetUserImpl();
}

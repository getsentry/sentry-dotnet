using System;
using System.Runtime.InteropServices;
using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Native;

/// <summary>
/// Scope Observer for Native through P/Invoke.
/// </summary>
/// <see href="https://github.com/getsentry/sentry-native"/>
internal class NativeScopeObserver : ScopeObserver
{
    public NativeScopeObserver(SentryOptions options) : base("Native", options) { }

    public override void AddBreadcrumbImpl(Breadcrumb breadcrumb)
    {
        // see https://develop.sentry.dev/sdk/event-payloads/breadcrumbs/
        var crumb = C.sentry_value_new_breadcrumb(breadcrumb.Type, breadcrumb.Message);
        C.sentry_value_set_by_key(crumb, "level", C.sentry_value_new_string(breadcrumb.Level.ToString().ToLower()));
        C.sentry_value_set_by_key(crumb, "timestamp", C.sentry_value_new_string(GetTimestamp(breadcrumb.Timestamp)));
        C.SetValueIfNotNull(crumb, "category", breadcrumb.Category);
        C.sentry_add_breadcrumb(crumb);
    }

    public override void SetExtraImpl(string key, string? value) =>
        C.sentry_set_extra(key, value is null ? C.sentry_value_new_null() : C.sentry_value_new_string(value));

    public override void SetTagImpl(string key, string value) => C.sentry_set_tag(key, value);

    public override void UnsetTagImpl(string key) => C.sentry_remove_tag(key);

    public override void SetUserImpl(SentryUser user)
    {
        // see https://develop.sentry.dev/sdk/event-payloads/user/
        var cUser = C.sentry_value_new_object();
        C.SetValueIfNotNull(cUser, "id", user.Id);
        C.SetValueIfNotNull(cUser, "username", user.Username);
        C.SetValueIfNotNull(cUser, "email", user.Email);
        C.SetValueIfNotNull(cUser, "ip_address", user.IpAddress);
        C.sentry_set_user(cUser);
    }

    public override void UnsetUserImpl() => C.sentry_remove_user();

    private static string GetTimestamp(DateTimeOffset timestamp) =>
        // "o": Using ISO 8601 to make sure the timestamp makes it to the bridge correctly.
        // https://docs.microsoft.com/en-gb/dotnet/standard/base-types/standard-date-and-time-format-strings#Roundtrip
        timestamp.ToString("o");
}

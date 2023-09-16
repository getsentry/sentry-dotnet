namespace Sentry.Desktop;

// https://github.com/getsentry/sentry-unity/blob/3eb6eca6ed270c5ec023bf75ee53c1ca00bb7c82/src/Sentry.Unity.Native/NativeScopeObserver.cs
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

    public override void SetUserImpl(User user)
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

    /// <summary>
    /// Sentry Unity Scope Observer wrapper for the common behaviour accross platforms.
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
            // var serialized = value is null ? null : SafeSerializer.SerializeSafely(value);
            // if (value is not null && serialized is null)
            // {
            //     _options.DiagnosticLogger?.Log(SentryLevel.Warning,
            //         "{0} Scope Sync - SetExtra k:\"{1}\" v:\"{2}\" - value was serialized as null",
            //         null, _name, key, value);
            // }
            // else
            // {
            //     _options.DiagnosticLogger?.Log(SentryLevel.Debug,
            //         "{0} Scope Sync - Setting Extra k:\"{1}\" v:\"{2}\"", null, _name, key, value);
            // }
            // SetExtraImpl(key, serialized);
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

        public void SetUser(User? user)
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

        public abstract void SetUserImpl(User user);

        public abstract void UnsetUserImpl();
    }

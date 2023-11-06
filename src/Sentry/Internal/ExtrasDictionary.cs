namespace Sentry.Internal;

internal class ExtrasDictionary : ScopeSyncingDictionary<string, object?>
{
    public ExtrasDictionary(IDictionary<string, object?> innerDictionary, SentryOptions options) : base(innerDictionary, options)
    {
    }

    public override void SyncSetValue(string key, object? value) => Options.ScopeObserver?.SetExtra(key, value);

    public override void SyncRemoveValue(string key) => Options.ScopeObserver?.UnsetExtra(key);
}

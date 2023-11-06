namespace Sentry.Internal;

internal class TagsDictionary : ScopeSyncingDictionary<string, string>
{
    public TagsDictionary(IDictionary<string, string> innerDictionary, SentryOptions options) : base(innerDictionary, options)
    {
    }

    public override bool FilterValue(string key, string value) => Options.TagFilters.Any(x => x.IsMatch(key));

    public override void SyncSetValue(string key, string value) => Options.ScopeObserver?.SetTag(key, value);

    public override void SyncRemoveValue(string key) => Options.ScopeObserver?.UnsetTag(key);
}

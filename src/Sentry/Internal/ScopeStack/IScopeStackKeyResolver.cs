namespace Sentry.Internal.ScopeStack;

internal interface IScopeStackKeyResolver
{
    public object? ScopeKey { get; }
}

internal static class ScopeStackKeyHelper
{
    private const string FusedScopeStackKey = "__FusedScopeStackKey";

    internal static object? GetScopeStackKey(this object activity) => activity.GetFused<object>(FusedScopeStackKey);

    internal static void SetScopeStackKey(this object activity, object? key) => activity.SetFused(FusedScopeStackKey, key);

    internal static void PropagateScopeStackKey(this object source, object target)
    {
        var activityScopeStackKey = source.GetFused<object>(FusedScopeStackKey);
        target.SetFused(FusedScopeStackKey, activityScopeStackKey);
    }
}

using Sentry.Internal.ScopeStack;

namespace Sentry.AspNetCore;

internal class ActivityRootIdKeyResolver: IScopeStackKeyResolver
{
    /// <summary>
    /// Uses the current <see cref="Activity"/> RootId as the scope key.
    /// </summary>
    public object? ScopeKey => Activity.Current?.RootId;
}

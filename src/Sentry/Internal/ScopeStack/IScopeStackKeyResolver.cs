namespace Sentry.Internal.ScopeStack;

internal interface IScopeStackKeyResolver
{
    public object? ScopeKey { get; }
}

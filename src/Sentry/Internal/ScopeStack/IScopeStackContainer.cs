namespace Sentry.Internal.ScopeStack;

internal interface IScopeStackContainer
{
    public KeyValuePair<Scope, ISentryClient>[]? Stack { get; set; }
}

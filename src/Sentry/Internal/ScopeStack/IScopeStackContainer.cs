namespace Sentry.Internal.ScopeStack;

internal interface IScopeStackContainer
{
    KeyValuePair<Scope, ISentryClient>[]? Stack { get; set; }
}

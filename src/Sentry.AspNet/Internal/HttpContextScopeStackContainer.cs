using System.Web;
using Sentry.Internal.ScopeStack;

namespace Sentry.AspNet.Internal;

internal class HttpContextScopeStackContainer : IScopeStackContainer
{
    private const string FieldName = "__SentryScopeStack";

    //Internal for testing
    internal KeyValuePair<Scope, ISentryClient>[]? FallbackStack;

    public KeyValuePair<Scope, ISentryClient>[]? Stack
    {
        get
        {
            if (HttpContext.Current is { } currentContext)
            {
                return currentContext.Items[FieldName] as KeyValuePair<Scope, ISentryClient>[];
            }
            return FallbackStack;
        }
        set
        {
            if (HttpContext.Current is { } currentContext)
            {
                currentContext.Items[FieldName] = value;
            }
            else
            {
                FallbackStack = value;
            }
        }
    }
}

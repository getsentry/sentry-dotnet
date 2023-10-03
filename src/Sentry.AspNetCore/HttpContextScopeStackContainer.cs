using Microsoft.AspNetCore.Http;
using Sentry.Internal.ScopeStack;

namespace Sentry.AspNetCore;

internal class HttpContextScopeStackContainer : IScopeStackContainer
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string FieldName = "__SentryScopeStack";

    //Internal for testing
    internal KeyValuePair<Scope, ISentryClient>[]? FallbackStack;

    public HttpContextScopeStackContainer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public KeyValuePair<Scope, ISentryClient>[]? Stack
    {
        get
        {
            if (_httpContextAccessor.HttpContext is { } currentContext)
            {
                return currentContext.Items[FieldName] as KeyValuePair<Scope, ISentryClient>[];
            }
            return FallbackStack;
        }
        set
        {
            if (_httpContextAccessor.HttpContext is { } currentContext)
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

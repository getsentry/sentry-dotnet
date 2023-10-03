using Microsoft.AspNetCore.Http;
using Sentry.Internal.ScopeStack;

namespace Sentry.AspNetCore;

internal class HttpRequestScopeKeyResolver: IScopeStackKeyResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpRequestScopeKeyResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Uses the current <see cref="HttpContext"/> as the scope key.
    /// </summary>
    public object? ScopeKey => _httpContextAccessor.HttpContext?.Request;
}

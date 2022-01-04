using Microsoft.AspNetCore.Http;

namespace Sentry.AspNetCore;

/// <summary>
/// WIP
/// </summary>
public interface ISentryRouteName
{
    /// <summary>
    /// Return the name of the route from the given context.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>The route name value or null if you want to keep this route as 'Unknown'.</returns>
    string? GetRouteName(HttpContext context);
}

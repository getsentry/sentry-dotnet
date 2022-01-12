using Microsoft.AspNetCore.Http;

namespace Sentry.AspNetCore;

/// <summary>
/// Provides the strategy to define the name of a transaction based on the HttpContext
/// </summary>
public interface ITransactionNameProvider
{
    /// <summary>
    /// The strategy to define the name of a transaction based on the HttpContext
    /// </summary>
    /// <remarks>
    /// The SDK can name transactions automatically when using MVC or Endpoint Routing. In other cases, like when serving static files, it fallback to Unknown Route. This hook allows custom code to define a transaction name given a HttpContext.
    /// </remarks>
    string? GetRouteName(HttpContext context);
}

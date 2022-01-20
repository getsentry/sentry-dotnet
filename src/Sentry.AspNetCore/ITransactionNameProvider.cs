using Microsoft.AspNetCore.Http;

namespace Sentry.AspNetCore;

/// <summary>
/// Provides the strategy to define the name of a transaction based on the <see cref="HttpContext"/>.
/// </summary>
/// <remarks>
/// The SDK can name transactions automatically when using MVC or Endpoint Routing. In other cases, like when serving static files, it fallback to Unknown Route. This hook allows custom code to define a transaction name given a <see cref="HttpContext"/>.
/// </remarks>
public delegate string? TransactionNameProvider(HttpContext context);

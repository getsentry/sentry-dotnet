using Microsoft.AspNetCore.Http;

namespace Sentry.AspNetCore;

/// <summary>
/// Sentry User Factory
/// </summary>
[Obsolete("This interface is tightly coupled to AspNetCore and will be removed in version 4.0.0. Please consider using ISentryUserFactory with IHttpContextAccessor instead.")]
public interface IUserFactory
{
    /// <summary>
    /// Creates a <see cref="User"/> from the <see cref="HttpContext"/>
    /// </summary>
    /// <param name="context">The HttpContext where the user resides</param>
    /// <returns>The protocol user</returns>
    User? Create(HttpContext context);
}

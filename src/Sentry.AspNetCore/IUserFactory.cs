using Microsoft.AspNetCore.Http;

namespace Sentry.AspNetCore;

/// <summary>
/// <para>
/// Sentry User Factory
/// </para>
/// <para>
/// Note: This interface is tightly coupled to AspNetCore and Will be removed in version 4.0.0. Please consider using
/// <see cref="ISentryUserFactory"/> with <see cref="IHttpContextAccessor"/> instead.
/// </para>
/// </summary>
public interface IUserFactory
{
    /// <summary>
    /// Creates a <see cref="User"/> from the <see cref="HttpContext"/>
    /// </summary>
    /// <param name="context">The HttpContext where the user resides</param>
    /// <returns>The protocol user</returns>
    User? Create(HttpContext context);
}

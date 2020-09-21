using Microsoft.AspNetCore.Http;
using Sentry.Protocol;

namespace Sentry.AspNetCore
{
    /// <summary>
    /// Sentry User Factory
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
}

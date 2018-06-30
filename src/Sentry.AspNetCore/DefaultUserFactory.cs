using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Sentry.Protocol;

namespace Sentry.AspNetCore
{
    internal class DefaultUserFactory : IUserFactory
    {
        public User Create(HttpContext context)
        {
            var principal = context.User;

            string email = null;
            string id = null;
            foreach (var claim in principal.Claims)
            {
                switch (claim.Type)
                {
                    case ClaimTypes.Email:
                        email = claim.Value;
                        break;
                    case ClaimTypes.NameIdentifier:
                        id = claim.Value;
                        break;
                }
            }

            string username = null;
            if (id == null)
            {
                id = principal.Identity?.Name;
            }
            else if (id != principal.Identity?.Name)
            {
                username = principal.Identity?.Name;
            }

            if (email != null || id != null || username != null)
            {
                return new User
                {
                    Id = id,
                    Email = email,
                    Username = username,
                    IpAddress = context.Connection.RemoteIpAddress?.ToString()
                };
            }

            return null;
        }
    }
}

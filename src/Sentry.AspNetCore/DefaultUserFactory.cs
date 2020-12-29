using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Sentry.Protocol;

namespace Sentry.AspNetCore
{
    internal class DefaultUserFactory : IUserFactory
    {
        public User? Create(HttpContext context)
        {
            var principal = context.User;
            if (principal == null)
            {
                return null;
            }

            string? email = null;
            string? id = null;
            string? username = null;
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
                    case ClaimTypes.Name:
                        username = claim.Value;
                        break;
                }
            }

            // Identity.Name Reads the value of: ClaimsIdentity.NameClaimType which by default is ClaimTypes.Name
            // It can be changed by the application to read a different claim though:
            var name = principal.Identity?.Name;
            if (name != null && username != name)
            {
                username = name;
            }

            // Don't create a user if all we have is his IP address
            return email == null && id == null && username == null
                ? null
                : new User
                {
                    Id = id,
                    Email = email,
                    Username = username,
                    IpAddress = context.Connection?.RemoteIpAddress?.ToString()
                };
        }
    }
}

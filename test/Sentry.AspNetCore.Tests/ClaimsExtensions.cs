using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Sentry.AspNetCore.Tests
{
    internal static class ClaimsExtensions
    {
        public static string NameIdentifier(this IEnumerable<Claim> claims)
            => claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        public static string Email(this IEnumerable<Claim> claims)
            => claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        public static string Name(this IEnumerable<Claim> claims)
            => claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
    }
}

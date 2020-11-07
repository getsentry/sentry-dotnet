using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Sentry.Internal.Http
{
    internal class RateLimit
    {
        public IReadOnlyList<RateLimitCategory> Categories { get; }

        public TimeSpan RetryAfter { get; }

        public RateLimit(
            IReadOnlyList<RateLimitCategory> categories,
            TimeSpan retryAfter)
        {
            Categories = categories;
            RetryAfter = retryAfter;
        }

        public static RateLimit Parse(string rateLimitEncoded)
        {
            // Don't remove empty entries because some components may be empty (e.g. categories)
            var components = rateLimitEncoded.Split(':');

            var retryAfter = TimeSpan.FromSeconds(int.Parse(components[0], CultureInfo.InvariantCulture));
            var categories = components[1].Split(';').Select(c => new RateLimitCategory(c)).ToArray();

            return new RateLimit(
                categories,
                retryAfter
            );
        }

        public static IEnumerable<RateLimit> ParseMany(string rateLimitsEncoded) =>
            rateLimitsEncoded.Split(',').Select(Parse);
    }
}

using System;
using Sentry.Protocol;

namespace Sentry.Internal.Http
{
    public class RateLimitCategory
    {
        public string Name { get; }

        public bool IsMatchAll => string.IsNullOrWhiteSpace(Name);

        public RateLimitCategory(string name) => Name = name;

        public bool Matches(EnvelopeItem item)
        {
            // Empty category name matches everything
            if (IsMatchAll)
            {
                return true;
            }

            var type = item.TryGetType();
            if (string.IsNullOrWhiteSpace(type))
            {
                return false;
            }

            return string.Equals(Name, type, StringComparison.OrdinalIgnoreCase);
        }
    }
}

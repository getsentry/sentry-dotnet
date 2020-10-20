using System;
using Sentry.Protocol;

namespace Sentry.Internal.Http
{
    public class RateLimitCategory
    {
        public string Name { get; }

        public bool IsMatchAll => string.IsNullOrWhiteSpace(Name);

        public RateLimitCategory(string name) => Name = name;

        public bool MatchesItem(EnvelopeItem item)
        {
            if (IsMatchAll)
            {
                return true;
            }

            var type = item.TryGetType();
            if (string.IsNullOrWhiteSpace(type))
            {
                return false;
            }

            // TODO: figure out how exactly categories map onto item types
            return string.Equals(Name, type, StringComparison.OrdinalIgnoreCase);
        }
    }
}

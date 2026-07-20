using Sentry.Protocol.Envelopes;

namespace Sentry.Internal.Http;

internal class RateLimitCategory : IEquatable<RateLimitCategory>
{
    public string Name { get; }

    public bool IsMatchAll => string.IsNullOrWhiteSpace(Name);

    public RateLimitCategory(string name) => Name = name;

    public bool Matches(EnvelopeItem item)
    {
        if (IsMatchAll)
        {
            return true;
        }

        // Rate limits are keyed by data category (e.g. "error", "monitor", "metric_bucket"), which is not
        // always the same as the envelope item type (e.g. "event", "check_in", "statsd"). Compare against the
        // item's data category so limits for those categories are honoured rather than silently ignored.
        return string.Equals(Name, item.DataCategory.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(RateLimitCategory? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((RateLimitCategory)obj);
    }

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name);
}

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

        var type = item.TryGetType()?.ToLower();
        if (string.IsNullOrWhiteSpace(type))
        {
            return false;
        }

        // The envelope item type for metrics is `statsd`, but the client report category is `metric_bucket`
        var reportCategory = EnvelopeItem.TypeValueMetric == type
            ? "metric_bucket"
            : type;

        return string.Equals(Name, reportCategory, StringComparison.OrdinalIgnoreCase);
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

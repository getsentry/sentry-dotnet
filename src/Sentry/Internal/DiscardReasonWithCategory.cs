using System;

namespace Sentry.Internal
{
    internal readonly struct DiscardReasonWithCategory :
        IEquatable<DiscardReasonWithCategory>, IComparable<DiscardReasonWithCategory>, IComparable
    {
        public DiscardReason Reason { get; }
        public DataCategory Category { get; }

        public DiscardReasonWithCategory(string reason, string category)
        {
            Reason = new DiscardReason(reason);
            Category = new DataCategory(category);
        }

        public DiscardReasonWithCategory(DiscardReason reason, DataCategory category)
        {
            Reason = reason;
            Category = category;
        }

        public int CompareTo(DiscardReasonWithCategory other)
        {
            var reasonComparison = Reason.CompareTo(other.Reason);
            return reasonComparison != 0 ? reasonComparison : Category.CompareTo(other.Category);
        }

        public int CompareTo(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return 1;
            }

            return obj is DiscardReasonWithCategory other
                ? CompareTo(other)
                : throw new ArgumentException($"Object must be of type {nameof(DiscardReasonWithCategory)}");
        }

        public bool Equals(DiscardReasonWithCategory other) =>
            Reason.Equals(other.Reason) && Category.Equals(other.Category);

        public override bool Equals(object? obj) =>
            obj is DiscardReasonWithCategory other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Reason.GetHashCode() * 397) ^ Category.GetHashCode();
            }
        }

        public override string ToString() => $"{{ Reason = \"{Reason}\", Category = \"{Category}\" }}";
    }
}

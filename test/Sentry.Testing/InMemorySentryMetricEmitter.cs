#nullable enable

namespace Sentry.Testing;

public sealed class InMemorySentryMetricEmitter : SentryMetricEmitter
{
    public List<MetricEntry> Entries { get; } = new();
    internal List<SentryMetric> Metrics { get; } = new();

    /// <inheritdoc />
    private protected override void CaptureMetric<T>(SentryMetricType type, string name, T value, string? unit, IEnumerable<KeyValuePair<string, object>>? attributes, Scope? scope) where T : struct
    {
        Entries.Add(MetricEntry.Create(type, name, value, unit, attributes, scope));
    }

    /// <inheritdoc />
    private protected override void CaptureMetric<T>(SentryMetricType type, string name, T value, string? unit, ReadOnlySpan<KeyValuePair<string, object>> attributes, Scope? scope) where T : struct
    {
        Entries.Add(MetricEntry.Create(type, name, value, unit, attributes.ToArray(), scope));
    }

    /// <inheritdoc />
    private protected override void CaptureMetric<T>(SentryMetric<T> metric) where T : struct
    {
        Metrics.Add(metric);
    }

    /// <inheritdoc />
    protected internal override void Flush()
    {
        // no-op
    }

    public sealed class MetricEntry : IEquatable<MetricEntry>
    {
        public static MetricEntry Create(SentryMetricType type, string name, object value, string? unit, IEnumerable<KeyValuePair<string, object>>? attributes, Scope? scope)
        {
            return new MetricEntry(type, name, value, unit, attributes is null ? ImmutableDictionary<string, object>.Empty : attributes.ToImmutableDictionary(), scope);
        }

        private MetricEntry(SentryMetricType type, string name, object value, string? unit, ImmutableDictionary<string, object> attributes, Scope? scope)
        {
            Type = type;
            Name = name;
            Value = value;
            Unit = unit;
            Attributes = attributes;
            Scope = scope;
        }

        public SentryMetricType Type { get; }
        public string Name { get; }
        public object Value { get; }
        public string? Unit { get; }
        public ImmutableDictionary<string, object> Attributes { get; }
        public Scope? Scope { get; }

        public void AssertEqual(SentryMetricType type, string name, object value)
        {
            var expected = Create(type, name, value, null, null, null);
            Assert.Equal(expected, this);
        }

        public void AssertEqual(SentryMetricType type, string name, object value, string? unit, IEnumerable<KeyValuePair<string, object>>? attributes, Scope? scope)
        {
            var expected = Create(type, name, value, unit, attributes, scope);
            Assert.Equal(expected, this);
        }

        public bool Equals(MetricEntry? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Type == other.Type
                && string.Equals(Name, other.Name, StringComparison.Ordinal)
                && Value.Equals(other.Value)
                && string.Equals(Unit, other.Unit, StringComparison.Ordinal)
                && Attributes.SequenceEqual(other.Attributes, AttributeEqualityComparer.Instance)
                && ReferenceEquals(Scope, other.Scope);
        }

        public override bool Equals(object? obj)
        {
            return obj is MetricEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            throw new UnreachableException();
        }
    }

    private sealed class AttributeEqualityComparer : IEqualityComparer<KeyValuePair<string, object>>
    {
        public static AttributeEqualityComparer Instance { get; } = new AttributeEqualityComparer();

        private AttributeEqualityComparer()
        {
        }

        public bool Equals(KeyValuePair<string, object> x, KeyValuePair<string, object> y)
        {
            return string.Equals(x.Key, y.Key, StringComparison.Ordinal)
                && Equals(x.Value, y.Value);
        }

        public int GetHashCode(KeyValuePair<string, object> obj)
        {
            return HashCode.Combine(obj.Key, obj.Value);
        }
    }
}

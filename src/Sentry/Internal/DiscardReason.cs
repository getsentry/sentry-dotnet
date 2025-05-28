namespace Sentry.Internal;

internal readonly struct DiscardReason : IEnumeration<DiscardReason>
{
    // See https://develop.sentry.dev/sdk/client-reports/ for list
    public static DiscardReason BeforeSend = new("before_send");
    public static DiscardReason BufferOverflow = new("buffer_overflow");
    public static DiscardReason CacheOverflow = new("cache_overflow");
    public static DiscardReason EventProcessor = new("event_processor");
    public static DiscardReason NetworkError = new("network_error");
    public static DiscardReason QueueOverflow = new("queue_overflow");
    public static DiscardReason RateLimitBackoff = new("ratelimit_backoff");
    public static DiscardReason SampleRate = new("sample_rate");

    private readonly string _value;

    string IEnumeration.Value => _value;

    public DiscardReason(string value) => _value = value;

    public DiscardReasonWithCategory WithCategory(DataCategory category) => new(this, category);

    public int CompareTo(DiscardReason other) =>
        string.Compare(_value, other._value, StringComparison.Ordinal);

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return 1;
        }

        return obj is DiscardReason other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(DiscardReason)}");
    }

    public bool Equals(DiscardReason other) => _value == other._value;

    public override bool Equals(object? obj) => obj is DiscardReason other && Equals(other);

    public override int GetHashCode() => _value.GetHashCode();

    public override string ToString() => _value;
}

namespace Sentry.Internal
{
    internal record DiscardReason : Enumeration
    {
        public static DiscardReason QueueOverflow = new("queue_overflow");
        public static DiscardReason CacheOverflow = new("cache_overflow");
        public static DiscardReason RateLimitBackoff = new("ratelimit_backoff");
        public static DiscardReason NetworkError = new("network_error");
        public static DiscardReason SampleRate = new("sample_rate");

        private DiscardReason(string value) : base(value)
        {
        }
    }
}

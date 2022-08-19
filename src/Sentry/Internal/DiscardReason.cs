namespace Sentry.Internal
{
    internal record DiscardReason(string Value) : Enumeration(Value)
    {
        // See https://develop.sentry.dev/sdk/client-reports/ for list
        public static DiscardReason BeforeSend = new("before_send");
        public static DiscardReason CacheOverflow = new("cache_overflow");
        public static DiscardReason EventProcessor = new("event_processor");
        public static DiscardReason TransactionProcessor = new("transaction_processor");
        public static DiscardReason NetworkError = new("network_error");
        public static DiscardReason QueueOverflow = new("queue_overflow");
        public static DiscardReason RateLimitBackoff = new("ratelimit_backoff");
        public static DiscardReason SampleRate = new("sample_rate");

        public DiscardReasonWithCategory WithCategory(DataCategory category) => new(this, category);
    }
}

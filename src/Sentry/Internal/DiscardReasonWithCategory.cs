namespace Sentry.Internal
{
    internal record DiscardReasonWithCategory(DiscardReason Reason, DataCategory Category)
    {
        public DiscardReason Reason { get; } = Reason;
        public DataCategory Category { get; } = Category;
    }
}

namespace Sentry.Internal
{
    internal record DiscardReasonWithCategory(DiscardReason Reason, DataCategory Category)
    {
        public DiscardReasonWithCategory(string reason, string category)
            : this(new DiscardReason(reason), new DataCategory(category))
        {
        }
    }
}

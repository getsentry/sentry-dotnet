namespace Sentry.Internal
{
    internal interface IDiscardedEventCounter
    {
        void IncrementCounter(DiscardReason queueOverflow, DataCategory itemDataCategory);
    }
}

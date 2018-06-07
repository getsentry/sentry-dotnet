namespace Sentry.Internal
{
    internal interface IBackgroundWorker
    {
        bool EnqueueEvent(SentryEvent @event);
        int QueuedItems { get; }
    }
}

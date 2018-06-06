namespace Sentry.Internals
{
    internal interface IBackgroundWorker
    {
        bool EnqueueEvent(SentryEvent @event);
        int QueuedItems { get; }
    }
}

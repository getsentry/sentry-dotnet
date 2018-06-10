namespace Sentry.Extensibility
{
    public interface IBackgroundWorker
    {
        bool EnqueueEvent(SentryEvent @event);
        int QueuedItems { get; }
    }
}

namespace Sentry.Testing;

public class InMemoryDiagnosticLogger : IDiagnosticLogger
{
    public ConcurrentQueue<Entry> Entries { get; } = new();

    public bool IsEnabled(SentryLevel level) => true;

    public void Log(SentryLevel logLevel, string message, Exception exception = null, params object[] args)
    {
        Entries.Enqueue(new Entry(logLevel, message, exception, args));
    }

    public Entry Dequeue()
    {
        if (Entries.TryDequeue(out var entry))
        {
            return entry;
        }

        throw new InvalidOperationException("Queue is empty.");
    }

    public record Entry(
        SentryLevel Level,
        string Message,
        Exception Exception,
        object[] Args);
}

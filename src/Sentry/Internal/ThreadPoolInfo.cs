using Sentry.Extensibility;

namespace Sentry.Internal;

internal sealed class ThreadPoolInfo : ISentryJsonSerializable
{
    public ThreadPoolInfo(
        int minWorkerThreads,
        int minCompletionPortThreads,
        int maxWorkerThreads,
        int maxCompletionPortThreads,
        int availableWorkerThreads,
        int availableCompletionPortThreads)
    {
        MinWorkerThreads = minWorkerThreads;
        MinCompletionPortThreads = minCompletionPortThreads;
        MaxWorkerThreads = maxWorkerThreads;
        MaxCompletionPortThreads = maxCompletionPortThreads;
        AvailableWorkerThreads = availableWorkerThreads;
        AvailableCompletionPortThreads = availableCompletionPortThreads;
    }

    public int MinWorkerThreads { get; }
    public int MinCompletionPortThreads { get; }
    public int MaxWorkerThreads { get; }
    public int MaxCompletionPortThreads { get; }
    public int AvailableWorkerThreads { get; }
    public int AvailableCompletionPortThreads { get; }

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteNumber("min_worker_threads", MinWorkerThreads);
        writer.WriteNumber("min_completion_port_threads", MinCompletionPortThreads);
        writer.WriteNumber("max_worker_threads", MaxWorkerThreads);
        writer.WriteNumber("max_completion_port_threads", MaxCompletionPortThreads);
        writer.WriteNumber("available_worker_threads", AvailableWorkerThreads);
        writer.WriteNumber("available_completion_port_threads", AvailableCompletionPortThreads);

        writer.WriteEndObject();
    }
}

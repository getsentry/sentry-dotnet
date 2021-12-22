using System.Text.Json;
using Sentry.Extensibility;

namespace Sentry
{
    internal sealed class ThreadPoolInfo : IJsonSerializable
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

            writer.WriteNumber("minWorkerThreads", MinWorkerThreads);
            writer.WriteNumber("minCompletionPortThreads", MinCompletionPortThreads);
            writer.WriteNumber("maxWorkerThreads", MaxWorkerThreads);
            writer.WriteNumber("maxCompletionPortThreads", MaxCompletionPortThreads);
            writer.WriteNumber("availableWorkerThreads", AvailableWorkerThreads);
            writer.WriteNumber("availableCompletionPortThreads", AvailableCompletionPortThreads);

            writer.WriteEndObject();
        }
    }
}

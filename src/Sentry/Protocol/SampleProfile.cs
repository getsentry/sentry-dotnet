using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

// A list of frame indexes.
using SentryProfileStackTrace = HashableGrowableArray<int>;

/// <summary>
/// Sentry sampling profiler output profile
/// </summary>
internal sealed class SampleProfile : IJsonSerializable
{
    public GrowableArray<Sample> Samples { get; } = new(10000);
    public GrowableArray<SentryStackFrame> Frames { get; } = new(100);
    public GrowableArray<SentryProfileStackTrace> Stacks { get; } = new(100);
    public List<SentryThread> Threads { get; } = new(10);

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();
        writer.WriteStartObject("thread_metadata");
        for (var i = 0; i < Threads.Count; i++)
        {
            writer.WriteSerializable(i.ToString(), Threads[i], logger);
        }
        writer.WriteEndObject();
        writer.WriteArray("stacks", Stacks, logger);
        writer.WriteArray("frames", Frames, logger);
        writer.WriteArray("samples", Samples, logger);
        writer.WriteEndObject();
    }

    public class Sample : IJsonSerializable
    {
        /// <summary>
        /// Timestamp in nanoseconds relative to the profile start.
        /// </summary>
        public ulong Timestamp;

        public int ThreadId;
        public int StackId;

        public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
        {
            writer.WriteStartObject();
            writer.WriteNumber("elapsed_since_start_ns", Timestamp);
            writer.WriteNumber("thread_id", ThreadId);
            writer.WriteNumber("stack_id", StackId);
            writer.WriteEndObject();
        }
    }
}

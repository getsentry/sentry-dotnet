using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

#if NETFRAMEWORK
using Sentry.PlatformAbstractions;
#endif

namespace Sentry.Protocol;

/// <summary>
/// Sentry sampling profiler output profile
/// </summary>
internal sealed class SampleProfile : IJsonSerializable
{
    // Note: changing these to properties would break because GrowableArray is a struct.
    internal Internal.GrowableArray<Sample> Samples = new(10000);
    internal Internal.GrowableArray<SentryStackFrame> Frames = new(100);
    internal Internal.GrowableArray<Internal.GrowableArray<int>> Stacks = new(100);
    internal List<SentryThread> Threads = new(10);

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();
        writer.WriteStartObject("thread_metadata");
        for (var i = 0; i < Threads.Count; i++)
        {
            writer.WriteSerializable(i.ToString(), Threads[i], logger);
        }
        writer.WriteEndObject();

#if NETFRAMEWORK
        if (PlatformAbstractions.SentryRuntime.Current.IsMono())
        {
            // STJ doesn't like HashableGrowableArray on Mono, failing with:
            //   Invalid IL code in (wrapper dynamic-method) object:.ctor (): IL_0005: ret
            // We can work around this by converting them to regular arrays.
            // (This appears fixed as of STJ 6.0.5, but that's too high of a minimal dependency for us.)
            // Probably we won't ever hit this for real, because we only support profiling on .NET 6+
            // but this allows the tests to pass.
            var stacks = Stacks.Select(s => s.ToArray());
            writer.WriteArray("stacks", stacks, logger);
        }
        else
        {
            writer.WriteArray("stacks", Stacks, logger);
        }
#else
        writer.WriteArray("stacks", Stacks, logger);
#endif
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

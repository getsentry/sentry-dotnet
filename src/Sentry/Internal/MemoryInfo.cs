#if NETCOREAPP3_0_OR_GREATER

using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internal;

internal sealed class MemoryInfo : ISentryJsonSerializable
{
    public long AllocatedBytes { get; }
    public long FragmentedBytes { get; }
    public long HeapSizeBytes { get; }
    public long HighMemoryLoadThresholdBytes { get; }
    public long TotalAvailableMemoryBytes { get; }
    public long MemoryLoadBytes { get; }

#if NET5_0_OR_GREATER
    public long TotalCommittedBytes { get; }
    public long PromotedBytes { get; }
    public long PinnedObjectsCount { get; }
    public double PauseTimePercentage { get; }
    public TimeSpan[] PauseDurations { get; }
    public long Index { get; }
    public long FinalizationPendingCount { get; }
    public bool Compacted { get; }
    public bool Concurrent { get; }

    public MemoryInfo(
        long allocatedBytes,
        long fragmentedBytes,
        long heapSizeBytes,
        long highMemoryLoadThresholdBytes,
        long totalAvailableMemoryBytes,
        long memoryLoadBytes,
        long totalCommittedBytes,
        long promotedBytes,
        long pinnedObjectsCount,
        double pauseTimePercentage,
        long index,
        long finalizationPendingCount,
        bool compacted,
        bool concurrent,
        TimeSpan[] pauseDurations)
    {
        AllocatedBytes = allocatedBytes;
        FragmentedBytes = fragmentedBytes;
        HeapSizeBytes = heapSizeBytes;
        HighMemoryLoadThresholdBytes = highMemoryLoadThresholdBytes;
        TotalAvailableMemoryBytes = totalAvailableMemoryBytes;
        MemoryLoadBytes = memoryLoadBytes;
        TotalCommittedBytes = totalCommittedBytes;
        PromotedBytes = promotedBytes;
        PinnedObjectsCount = pinnedObjectsCount;
        PauseTimePercentage = pauseTimePercentage;
        PauseDurations = pauseDurations;
        Index = index;
        FinalizationPendingCount = finalizationPendingCount;
        Compacted = compacted;
        Concurrent = concurrent;
    }
#else
    public MemoryInfo(
        long allocatedBytes,
        long fragmentedBytes,
        long heapSizeBytes,
        long highMemoryLoadThresholdBytes,
        long totalAvailableMemoryBytes,
        long memoryLoadBytes)
    {
        AllocatedBytes = allocatedBytes;
        FragmentedBytes = fragmentedBytes;
        HeapSizeBytes = heapSizeBytes;
        HighMemoryLoadThresholdBytes = highMemoryLoadThresholdBytes;
        TotalAvailableMemoryBytes = totalAvailableMemoryBytes;
        MemoryLoadBytes = memoryLoadBytes;
    }
#endif
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        //WriteNumberIfNotZero since on OS that dont implement all props, those props are stubbed out to zero
        writer.WriteStartObject();

        writer.WriteNumberIfNotZero("allocated_bytes", AllocatedBytes);
        writer.WriteNumberIfNotZero("fragmented_bytes", FragmentedBytes);
        writer.WriteNumberIfNotZero("heap_size_bytes", HeapSizeBytes);
        writer.WriteNumberIfNotZero("high_memory_load_threshold_bytes", HighMemoryLoadThresholdBytes);
        writer.WriteNumberIfNotZero("total_available_memory_bytes", TotalAvailableMemoryBytes);
        writer.WriteNumberIfNotZero("memory_load_bytes", MemoryLoadBytes);

#if NET5_0_OR_GREATER
        writer.WriteNumberIfNotZero("total_committed_bytes", TotalCommittedBytes);
        writer.WriteNumberIfNotZero("promoted_bytes", PromotedBytes);
        writer.WriteNumberIfNotZero("pinned_objects_count", PinnedObjectsCount);
        writer.WriteNumberIfNotZero("pause_time_percentage", PauseTimePercentage);
        writer.WriteNumberIfNotZero("index", Index);
        writer.WriteNumber("finalization_pending_count", FinalizationPendingCount);
        writer.WriteBoolean("compacted", Compacted);
        writer.WriteBoolean("concurrent", Concurrent);
        writer.WritePropertyName("pause_durations");
        writer.WriteStartArray();
        foreach (var duration in PauseDurations)
        {
            writer.WriteNumberValue(duration.TotalMilliseconds);
        }
        writer.WriteEndArray();
#endif
        writer.WriteEndObject();
    }
}

#endif
